using System.Data.Common;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EDMXMigrationTool
{
    public partial class frmCentralForm : Form
    {
        private const string DefaultSchema = "General";
        public frmCentralForm()
        {
            InitializeComponent();
        }
        public void bntFileSelection_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "EDMX Files (*.edmx)|*.edmx";
            openFileDialog.Title = "Select an EDMX File";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtFile.Text = openFileDialog.FileName;
            }
        }
        public void btnFindDestination_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Select the destination folder for the migration files";
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtDestination.Text = folderBrowserDialog.SelectedPath;
            }
        }
        public void btnRun_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFile.Text) || string.IsNullOrWhiteSpace(txtDestination.Text))
            {
                MessageBox.Show("Please select both an EDMX file and a destination folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bntFileSelection.Enabled = false;
            btnFindDestination.Enabled = false;
            btnRun.Enabled = false;
            bkWorker.RunWorkerAsync(new UIParameters()
            {
                FilePath = txtFile.Text,
                DestinationPath = txtDestination.Text,
                Namespace = txtNamespace.Text,
                CreateDbContext = chkCreateDBContext.Checked,
                CreateModels = chkCreateModels.Checked,
                CreateRepositories = chkCreateRepositories.Checked,
                CreateConfigurations = chkCreateConfigurations.Checked
,
            });
        }
        private void AddLog(string message)
        {
            txtLog.BeginInvoke(new Action(() =>
            {
                txtLog.AppendText($"{DateTime.Now:hh:mm:ss tt}: {message}{Environment.NewLine}");
                txtLog.ScrollToCaret();
            }));
        }
        private void bkWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                UIParameters parameters = (UIParameters)e.Argument ?? new UIParameters();
                AddLog("Starting migration...");
                //1. Load the EDMX file
                string edmxFilePath = txtFile.Text;
                if (!File.Exists(edmxFilePath))
                {
                    throw new FileNotFoundException("The specified EDMX file does not exist.", edmxFilePath);
                }
                AddLog($"Loading EDMX file: {edmxFilePath}");
                var edmxContent = File.ReadAllText(edmxFilePath);
                //2. Parse the EDMX file
                AddLog("Parsing EDMX file...");
                var edmxDocument = XDocument.Parse(edmxContent);
                //3. Migrate the EDMX file
                AddLog("Migrating EDMX file...");
                //4. Listing all entities and their properties
                XElement? edmxRuntime = edmxDocument?.Descendants().FirstOrDefault(n => n.Name.LocalName == "Runtime");
                if (edmxRuntime == null)
                {
                    throw new InvalidOperationException("The EDMX file does not contain any entities.");
                }
                AddLog(" --> Reading Tables:");
                IDictionary<string, Table> tables = GetStorageModels(edmxRuntime);
                AddLog($" --> Tables:{tables.Count}");
                AddLog(" --> Reading Entities:");
                IDictionary<string, Entity> entities = GetConceptualModels(edmxRuntime);
                AddLog($" --> Entities:{entities.Count}");
                AddLog(" --> Reading mappings:");
                IList<Mapping> mappings = GetMappings(edmxRuntime);
                AddLog($" --> Mappings:{mappings.Count}");
                AddLog("Analysing:");
                AnaliceContext(tables, entities, mappings);

                AddLog("Creating the schema enum:");
                List<string> schemas = CreateSchemaClass(tables, parameters);

                AddLog("Creating folders:");
                CreateFolder(Path.Combine(parameters.DestinationPath, "Configuration"));
                CreateFolder(Path.Combine(parameters.DestinationPath, "Domain"));
                CreateFolder(Path.Combine(parameters.DestinationPath, "IRepositories"));
                CreateFolder(Path.Combine(parameters.DestinationPath, "Repositories"));
                foreach (var schema in schemas.Where(x => x != "dbo"))
                {
                    CreateFolder(Path.Combine(parameters.DestinationPath, "Configuration", schema));
                    CreateFolder(Path.Combine(parameters.DestinationPath, "Domain", schema));
                    CreateFolder(Path.Combine(parameters.DestinationPath, "IRepositories", schema));
                    CreateFolder(Path.Combine(parameters.DestinationPath, "Repositories", schema));
                }
                CreateFolder(Path.Combine(parameters.DestinationPath, "Configuration", DefaultSchema));
                CreateFolder(Path.Combine(parameters.DestinationPath, "Domain", DefaultSchema));
                CreateFolder(Path.Combine(parameters.DestinationPath, "IRepositories", DefaultSchema));
                CreateFolder(Path.Combine(parameters.DestinationPath, "Repositories", DefaultSchema));

                if (parameters.CreateConfigurations)
                {
                    AddLog("Creating configuration classes:");
                    CreateConfigurationClasses(tables, entities, mappings, parameters);
                }
                if (parameters.CreateModels)
                {
                    AddLog("Creating models classes:");
                    CreateModelClasses(tables, entities, mappings, parameters);
                }
                if (parameters.CreateDbContext)
                {
                    //AddLog("Creating the DBContext with dbSets:");
                    //CreateDbContext(entities, parameters, "AppDbContext2", true, schemas);
                    AddLog("Creating the DBContext without dbSets:");
                    CreateDbContext(entities, parameters, "AppDbContext", false, schemas);
                }
                if (parameters.CreateRepositories)
                {
                    AddLog("Creating repositories classes:");
                    CreateRepositoriesClasses(tables, entities, mappings, parameters);
                }
                AddLog("Migrated!");
            }
            catch (Exception ex)
            {
                AddLog($"Error during migration: {ex.Message}");
            }
        }
        private List<string> CreateSchemaClass(IDictionary<string, Table> tables, UIParameters parameters)
        {
            StringBuilder file = new StringBuilder();
            file.AppendLine("using System;");
            file.Append(Environment.NewLine);
            file.Append("namespace ");
            file.Append(parameters.Namespace);
            file.AppendLine("{");
            file.AppendLine("   public class SchemaName {");
            List<string> schemas = tables.Values.Select(x => x.Schema).Distinct().ToList();
            foreach (string schema in schemas)
            {
                file.Append("       public const string ");
                if (schema.Equals("dbo", StringComparison.InvariantCultureIgnoreCase))
                {
                    file.AppendLine("General = \"dbo\";");
                }
                else
                {
                    file.Append(NameInPascalCase(schema));
                    file.Append(" = \"");
                    file.Append(schema);
                    file.AppendLine("\";");
                }
            }
            file.AppendLine("   }");
            file.AppendLine("}");
            File.WriteAllText(Path.Combine(txtDestination.Text, "SchemaName.cs"), file.ToString());
            return schemas;
        }
        private bool HasSchema(string name)
        {
            return !string.IsNullOrEmpty(name) && !name.Equals("dbo");
        }
        private void CreateRepositoriesClasses(IDictionary<string, Table> tables, IDictionary<string, Entity> entities, IList<Mapping> mappings, UIParameters parameters)
        {
            foreach (Entity entity in entities.Values.Where(x => x.Used))
            {
                bool hasSchema = HasSchema(entity.Schema);
                StringBuilder interfaceFile = new StringBuilder();
                //interfaceFile.AppendLine("using System;");
                //interfaceFile.AppendLine("using System.Collections.Generic;");
                //interfaceFile.AppendLine("using System.Threading.Tasks;");
                interfaceFile.Append("using ");
                interfaceFile.Append(parameters.Namespace);
                interfaceFile.AppendLine(".Contract;");
                //interfaceFile.Append("using ");
                //interfaceFile.Append(parameters.Namespace);
                //interfaceFile.Append(".Domain.");
                //if (hasSchema)
                //{
                //    interfaceFile.Append(entity.Schema);
                //}
                //else
                //{
                //    interfaceFile.Append(DefaultSchema);
                //}
                //interfaceFile.AppendLine(";");
                interfaceFile.Append(Environment.NewLine);
                interfaceFile.Append("namespace ");
                interfaceFile.Append(parameters.Namespace);
                interfaceFile.Append(".Contracts.Repositories.");
                if (hasSchema)
                {
                    interfaceFile.Append(entity.Schema);
                }
                else
                {
                    interfaceFile.Append(DefaultSchema);
                }
                interfaceFile.AppendLine(" {");
                interfaceFile.Append("   public interface I");
                interfaceFile.Append(entity.NameFixed);
                interfaceFile.Append("Repository : IRepository<Domain.");
                if (hasSchema)
                {
                    interfaceFile.Append(entity.Schema);
                }
                else
                {
                    interfaceFile.Append(DefaultSchema);
                }
                interfaceFile.Append(".");
                interfaceFile.Append(entity.NameFixed);
                interfaceFile.AppendLine("> {");
                interfaceFile.AppendLine("   }");
                interfaceFile.Append("}");
                if (hasSchema)
                {
                    File.WriteAllText(Path.Combine(parameters.DestinationPath, "IRepositories", entity.Schema, "I" + entity.NameFixed + "Repository.cs"), interfaceFile.ToString());
                }
                else
                {
                    File.WriteAllText(Path.Combine(parameters.DestinationPath, "IRepositories", DefaultSchema, "I" + entity.NameFixed + "Repository.cs"), interfaceFile.ToString());
                }

                StringBuilder repoFile = new StringBuilder();
                //repoFile.AppendLine("using System;");
                //repoFile.AppendLine("using System.Collections.Generic;");
                //repoFile.AppendLine("using System.Threading.Tasks;");
                //repoFile.Append("using ");
                //repoFile.Append(parameters.Namespace);
                //repoFile.Append(".Domain.");
                //if (hasSchema)
                //{
                //    repoFile.Append(entity.Schema);
                //}
                //else
                //{
                //    repoFile.Append(DefaultSchema);
                //}
                //repoFile.AppendLine(";");
                repoFile.Append("using ");
                repoFile.Append(parameters.Namespace);
                repoFile.Append(".Contracts.Repositories.");
                if (hasSchema)
                {
                    repoFile.Append(entity.Schema);
                }
                else
                {
                    repoFile.Append(DefaultSchema);
                }
                repoFile.AppendLine(";");
                repoFile.Append(Environment.NewLine);
                repoFile.Append("namespace ");
                repoFile.Append(parameters.Namespace);
                repoFile.Append(".Repositories.");
                if (hasSchema)
                {
                    repoFile.Append(entity.Schema);
                }
                else
                {
                    repoFile.Append(DefaultSchema);
                }
                repoFile.AppendLine(" {");
                repoFile.AppendLine($"   public class {entity.NameFixed}Repository : Repository<Domain.{(hasSchema? entity.Schema: DefaultSchema)}.{entity.NameFixed}>, I{entity.NameFixed}Repository {{");
                repoFile.AppendLine($"       public {entity.NameFixed}Repository(AppDbContext context) : base(context) {{");
                repoFile.AppendLine("       }");
                repoFile.AppendLine("   }");
                repoFile.AppendLine("}");
                if (hasSchema)
                {
                    File.WriteAllText(Path.Combine(parameters.DestinationPath, "Repositories", entity.Schema, entity.NameFixed + "Repository.cs"), repoFile.ToString());
                }
                else
                {
                    File.WriteAllText(Path.Combine(parameters.DestinationPath, "Repositories", DefaultSchema, entity.NameFixed + "Repository.cs"), repoFile.ToString());
                }
            }
        }
        private void CreateModelClasses(IDictionary<string, Table> tables, IDictionary<string, Entity> entities, IList<Mapping> mappings, UIParameters parameters)
        {
            foreach (Entity entity in entities.Values.Where(x => x.Used))
            {
                bool hasSchema = HasSchema(entity.Schema);
                StringBuilder file = new StringBuilder();
                file.AppendLine("using System;");
                // file.Append("using ");
                // file.Append(parameters.Namespace);
                // file.Append(".Domain;");

                file.Append(Environment.NewLine);
                file.Append("namespace ");
                file.Append(parameters.Namespace);
                file.Append(".Domain.");
                if (hasSchema)
                {
                    file.Append(entity.Schema);
                }
                else
                {
                    file.Append(DefaultSchema);
                }
                file.AppendLine(" {");
                file.Append("   public class ");
                file.Append(entity.NameFixed);
                file.AppendLine(" : Entity {");
                file.Append("       public ");
                file.Append(entity.NameFixed);
                file.AppendLine("() { }");
                file.Append(Environment.NewLine);
                foreach (Property property in entity.Properties)
                {
                    file.Append("       public ");
                    switch (property.Type)
                    {
                        case "String":
                            file.Append("string");
                            break;
                        case "Int32":
                            file.Append("int");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "Int64":
                            file.Append("long");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "Boolean":
                            file.Append("bool");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "DateTime":
                            file.Append("DateTime");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "Decimal":
                            file.Append("decimal");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "Double":
                            file.Append("double");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "Guid":
                            file.Append("Guid");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "Byte[]":
                            file.Append("byte[]");
                            break;
                        case "Byte":
                            file.Append("byte");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "Int16":
                            file.Append("short");
                            if (property.IsNullable)
                            {
                                file.Append("?");
                            }
                            break;
                        case "Binary":
                            file.Append("byte[]");
                            break;
                        default:
                            throw new Exception($"Unsupported type: {property.Type}");
                    }
                    file.Append(" ");
                    file.Append(property.Name);
                    file.AppendLine("  { get; set; }");
                }
                file.AppendLine("       public override string ToString(){");
                file.Append("           return $\"");
                file.Append(string.Join(" - ", entity.Properties.Select(p => "{" + p.Name + "}")));
                file.AppendLine("\";");
                file.AppendLine("       }");
                file.AppendLine("   }");
                file.Append("}");
                if (hasSchema)
                {
                    File.WriteAllText(Path.Combine(parameters.DestinationPath, "Domain", entity.Schema, entity.NameFixed + ".cs"), file.ToString());
                }
                else
                {
                    File.WriteAllText(Path.Combine(parameters.DestinationPath, "Domain", DefaultSchema, entity.NameFixed + ".cs"), file.ToString());
                }
            }
        }
        private void CreateFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    AddLog($"Created folder: {path}");
                }
                else
                {
                    AddLog($"Folder already exists: {path}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Failed to create folder '{path}': {ex.Message}");
                throw;
            }
        }
        private void CreateConfigurationClasses(IDictionary<string, Table> tables, IDictionary<string, Entity> entities, IList<Mapping> mappings, UIParameters parameters)
        {
            foreach (var mapping in mappings)
            {
                Table table = tables[mapping.TableName];
                Entity entity = entities[mapping.EntityName];
                bool hasSchema = HasSchema(table.Schema);
                StringBuilder file = new StringBuilder();
                //file.AppendLine("using System;");
                file.AppendLine("using Microsoft.EntityFrameworkCore;");
                file.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
                //file.Append("using ");
                //file.Append(parameters.Namespace);
                //file.Append(".Domain.");
                //if (hasSchema)
                //{
                //    file.Append(table.Schema);
                //}
                //else
                //{
                //    file.Append(DefaultSchema);
                //}
                //file.AppendLine(";");
                file.Append(Environment.NewLine);
                file.Append("namespace ");
                file.Append(parameters.Namespace);
                file.Append(".Configuration.");
                if (hasSchema)
                {
                    file.Append(table.Schema);
                }
                else
                {
                    file.Append(DefaultSchema);
                }
                file.AppendLine(" {");
                file.Append("   public class ");
                file.Append(table.EntityName);
                file.Append("Configuration : IEntityTypeConfiguration<Domain.");
                if (hasSchema)
                {
                    file.Append(table.Schema);
                }
                else
                {
                    file.Append(DefaultSchema);
                }
                file.Append(".");
                file.Append(table.EntityName);
                file.AppendLine(">{");
                file.Append("       public void Configure(EntityTypeBuilder<Domain.");
                if (hasSchema)
                {
                    file.Append(table.Schema);
                }
                else
                {
                    file.Append(DefaultSchema);
                }
                file.Append(".");
                file.Append(table.EntityName);
                file.AppendLine("> builder){");
                file.Append("           builder.ToTable(\"");
                file.Append(table.Name);
                file.Append("\", SchemaName.");
                if (string.Equals(table.Schema, "dbo", StringComparison.InvariantCultureIgnoreCase))
                {
                    file.Append("General");
                }
                else
                {
                    file.Append(table.Schema);
                }
                file.AppendLine(");");
                var primaryKeys = table.Columns.Values.Where(c => c.IsPrimaryKey).ToList();
                if (primaryKeys.Count > 1)
                {
                    file.Append("           builder.HasKey(x => new { ");
                    for (int i = 0; i < primaryKeys.Count; i++)
                    {
                        if (i > 0)
                        {
                            file.Append(", ");
                        }
                        file.Append("x.");
                        file.Append(primaryKeys[i].PropertyName);
                    }
                    file.AppendLine(" });");
                }
                else if (primaryKeys.Count == 1)
                {
                    file.Append("           builder.HasKey(x => x.");
                    file.Append(primaryKeys[0].PropertyName);
                    file.AppendLine(");");
                }

                foreach (MappingProperty prop in mapping.Properties)
                {
                    Column column = table.Columns[prop.ColumnName];
                    file.Append("           builder.Property(x => x.");
                    file.Append(prop.PropertyName);
                    file.Append(")");
                    if (!column.IsNullable)
                    {
                        file.Append(".IsRequired()");
                    }
                    if(prop.PropertyName != prop.ColumnName)
                    {
                        file.Append(".HasColumnName(\"");
                        file.Append(prop.ColumnName);
                        file.Append("\")");
                    }
                    if (column.MaxLength.HasValue && column.MaxLength.Value > 0)
                    {
                        file.Append(".HasMaxLength(");
                        file.Append(column.MaxLength.Value);
                        file.Append(")");
                    }
                    if (column.Precision.HasValue && column.Precision.Value > 0)
                    {
                        file.Append(".HasPrecision(");
                        file.Append(column.Precision.Value);
                        if (column.Scale.HasValue && column.Scale.Value > 0)
                        {
                            file.Append(", ");
                            file.Append(column.Scale.Value);
                        }
                        file.Append(")");
                    }
                    if (column.ValueGenerator == "Computed")
                    {
                        file.Append(".ValueGeneratedOnAddOrUpdate()");
                    }
                    else if (column.ValueGenerator == "Identity")
                    {
                        file.Append(".ValueGeneratedOnAdd()");
                    }
                    file.AppendLine(";");
                }

                file.AppendLine("       }");
                file.AppendLine("   }");
                file.AppendLine("}");
                if (hasSchema)
                {
                    File.WriteAllText(Path.Combine(parameters.DestinationPath, "Configuration", table.Schema, table.EntityName + ".cs"), file.ToString());
                }
                else
                {
                    File.WriteAllText(Path.Combine(parameters.DestinationPath, "Configuration", DefaultSchema, table.EntityName + ".cs"), file.ToString());
                }
            }
        }
        private void CreateDbContext(IDictionary<string, Entity> entities, UIParameters parameters, string name, bool hasDbSets, List<string> schemas)
        {
            StringBuilder file = new StringBuilder();
            //file.AppendLine("using System;");
            //file.AppendLine("using System.Collections.Generic;");
            //file.AppendLine("using System.Data;");
            //file.AppendLine("using System.Data.Common;");
            //file.AppendLine("using System.Linq;");
            //file.AppendLine("using System.Threading;");
            //file.AppendLine("using System.Threading.Tasks;");
            file.AppendLine("using Microsoft.EntityFrameworkCore;");
            //file.AppendLine("using Microsoft.EntityFrameworkCore.ChangeTracking;");
            //file.AppendLine("using Microsoft.Extensions.Logging;");
            //file.Append("using ");
            //file.Append(parameters.Namespace);
            //file.AppendLine(".Configuration;");
            //foreach (string schema in schemas.Where(x => x != "dbo"))
            //{
            //    file.Append("using ");
            //    file.Append(parameters.Namespace);
            //    file.Append(".Configuration.");
            //    file.Append(schema);
            //    file.AppendLine(";");
            //}
            file.Append(Environment.NewLine);
            file.Append("namespace ");
            file.Append(parameters.Namespace);
            file.AppendLine("{");
            file.AppendLine("   public class AppDbContext : DbContext{");
            file.AppendLine("       public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){");
            file.AppendLine("       }");
            file.AppendLine("       protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){");
            file.AppendLine("           base.OnConfiguring(optionsBuilder);");
            file.AppendLine("       }");
            if (hasDbSets)
            {
                foreach (Entity entity in entities.Values.Where(x => x.Used).OrderBy(x => x.Schema).ThenBy(x => x.Name))
                {
                    file.Append("       public DbSet<");
                    file.Append(entity.NameFixed);
                    file.Append("> ");
                    file.Append(entity.NameFixed);
                    if (
                        entity.NameFixed.EndsWith("s", StringComparison.InvariantCultureIgnoreCase)
                        //|| entity.NameFixed.EndsWith("1", StringComparison.InvariantCultureIgnoreCase)
                        )
                    {
                        //we dont need to do somthing else...
                    }
                    else if (entity.NameFixed.EndsWith("a", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("e", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("i", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("o", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("u", StringComparison.InvariantCultureIgnoreCase)


                        || entity.NameFixed.EndsWith("h", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("k", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("c", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("f", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("g", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("r", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("b", StringComparison.InvariantCultureIgnoreCase)
                        || entity.NameFixed.EndsWith("t", StringComparison.InvariantCultureIgnoreCase)
                        )
                    {
                        file.Append("s");
                    }
                    else if (entity.NameFixed.EndsWith("z", StringComparison.InvariantCultureIgnoreCase))
                    {
                        file.Length--;
                        file.Append("ces");
                    }
                    else
                    {
                        file.Append("es");
                    }
                    file.AppendLine(" { get; set; }");
                }
            }
            file.Append(Environment.NewLine);
            file.AppendLine("       protected override void OnModelCreating(ModelBuilder modelBuilder){");
            file.AppendLine("           base.OnModelCreating(modelBuilder);");
            foreach (Entity entity in entities.Values.Where(x => x.Used).OrderBy(x => x.Schema).ThenBy(x => x.Name))
            {
                file.Append("           modelBuilder.ApplyConfiguration(new Configuration.");
                if (HasSchema(entity.Schema))
                {
                    file.Append(entity.Schema);
                }
                else
                {
                    file.Append(DefaultSchema);
                }
                file.Append(".");
                file.Append(entity.NameFixed);
                file.AppendLine("Configuration());");
            }
            file.AppendLine("       }");
            file.AppendLine("   }");
            file.Append("}");
            File.WriteAllText(Path.Combine(parameters.DestinationPath, name + ".cs"), file.ToString());
        }
        private void AnaliceContext(IDictionary<string, Table> tables, IDictionary<string, Entity> entities, IList<Mapping> mappings)
        {
            foreach (Mapping map in mappings)
            {
                Table table;
                Entity entity;
                if (tables.ContainsKey(map.TableName))
                {
                    table = tables[map.TableName];
                }
                else
                {
                    throw new InvalidOperationException($"The table '{map.TableName}' is not defined in the SSDL context.");
                }
                if (entities.ContainsKey(map.EntityName))
                {
                    entity = entities[map.EntityName];
                }
                else
                {
                    throw new InvalidOperationException($"The entity '{map.EntityName}' is not defined in the C-S mapping context.");
                }
                table.Used = true;
                entity.Used = true;

                table.EntityName = entity.NameFixed;
                entity.TableName = map.TableName;
                entity.Schema = table.Schema;
                foreach (MappingProperty item in map.Properties)
                {
                    table.Columns[item.ColumnName].PropertyName = item.PropertyName;
                }
            }
        }
        /// <summary>
        /// SSDL content
        /// </summary>
        /// <returns>Tables list</returns>
        private IDictionary<string, Table> GetStorageModels(XElement? edmxRuntime)
        {
            IDictionary<string, Table> data = new Dictionary<string, Table>();
            XElement? storageModels = edmxRuntime?.Descendants().FirstOrDefault(n => n.Name.LocalName == "StorageModels");
            XElement? schema = storageModels?.Descendants().FirstOrDefault(n => n.Name.LocalName == "Schema");
            if (schema == null)
            {
                throw new InvalidOperationException("The EDMX file does not contain any schemas for SSDL context.");
            }
            foreach (var entity in schema.Descendants())
            {
                if (entity.Name.LocalName == "EntityType")
                {
                    Table table = new Table
                    {
                        Name = entity.Attribute("Name")?.Value ?? string.Empty
                    };
                    table.NameFixed = NameInPascalCase(table.Name);
                    foreach (XElement? property in entity.Descendants().Where(n => n.Name.LocalName == "Property"))
                    {
                        Column column = new Column
                        {
                            Name = property.Attribute("Name")?.Value ?? string.Empty,
                            Type = property.Attribute("Type")?.Value ?? string.Empty,
                            IsNullable = !(property.Attribute("Nullable")?.Value == "false"),
                            DefaultValue = property.Attribute("DefaultValue")?.Value,
                            MaxLength = Convert.ToInt32(property.Attribute("MaxLength")?.Value ?? "0"),
                            Precision = Convert.ToInt32(property.Attribute("Precision")?.Value ?? "0"),
                            Scale = Convert.ToInt32(property.Attribute("Scale")?.Value ?? "0"),
                            ValueGenerator = property.Attribute("StoreGeneratedPattern")?.Value,
                        };
                        column.MaxLength = column.MaxLength == 0 ? null : column.MaxLength == -1 ? (int?)null : column.MaxLength;
                        column.MaxLength = column.Type == "varchar(max)" ? null : column.MaxLength;
                        column.MaxLength = column.Type == "varbinary(max)" ? null : column.MaxLength;
                        column.IsPrimaryKey = entity.Descendants().Any(n => n.Name.LocalName == "Key" && n.Descendants().Any(k => k.Attribute("Name")?.Value == column.Name));
                        table.Columns.Add(column.Name, column);
                    }
                    data[table.Name] = table;
                }
            }
            XElement? schemaNames = storageModels?.Descendants().FirstOrDefault(n => n.Name.LocalName == "EntityContainer");
            foreach (var entity in schema.Descendants())
            {
                if (entity.Name.LocalName == "EntitySet")
                {
                    string entityName = entity.Attribute("Name")?.Value ?? string.Empty;
                    if (data.ContainsKey(entityName))
                    {
                        Table table = data[entityName];
                        table.Schema = entity.Attribute("Schema")?.Value ?? "dbo";
                    }
                }
            }
            return data;
        }
        private string NameInPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            if (IsInPascalCase(name))
            {
                return name.Replace("_", string.Empty);
            }
            // Replace underscores and spaces with a single space
            string[] parts = name
                .Replace("_", " ")
                .Replace("-", " ")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Convert each part to PascalCase
            var pascalName = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length == 0) continue;
                // Lowercase all except first letter
                pascalName.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                    pascalName.Append(part.Substring(1).ToLower());
            }
            string nameInPascalCase = pascalName.ToString();
            nameInPascalCase = nameInPascalCase.Replace("tms", "TMS", StringComparison.InvariantCultureIgnoreCase);
            nameInPascalCase = nameInPascalCase.EndsWith("doc") ? nameInPascalCase.Replace("doc", "Doc", StringComparison.InvariantCultureIgnoreCase) : nameInPascalCase;
            return nameInPascalCase;
        }
        private bool IsInPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            return char.IsUpper(name[0]) && (char.IsLower(name[1]) || (name.Length > 5 && char.IsLower(name[5])));
        }
        /// <summary>
        /// SSDL content
        /// </summary>
        /// <returns>Tables list</returns>
        private IDictionary<string, Entity> GetConceptualModels(XElement? edmxRuntime)
        {
            IDictionary<string, Entity> data = new Dictionary<string, Entity>();
            XElement? conceptualModels = edmxRuntime?.Descendants().FirstOrDefault(n => n.Name.LocalName == "ConceptualModels");
            XElement? schema = conceptualModels?.Descendants().FirstOrDefault(n => n.Name.LocalName == "Schema");
            if (schema == null)
            {
                throw new InvalidOperationException("The EDMX file does not contain any schemas for SSDL context.");
            }
            foreach (var entityType in schema.Descendants().Where(n => n.Name.LocalName == "EntityType"))
            {
                Entity entity = new Entity
                {
                    Name = entityType.Attribute("Name")?.Value ?? string.Empty,
                    TableName = entityType.Attribute("Name")?.Value ?? string.Empty,
                    Schema = string.Empty
                };
                if (entity.Name == "Regla1")
                {
                    entity.Name = "REGLA";
                }
                else if (entity.Name == "REGLA")
                {
                    entity.Name = "Regla1";
                }
                entity.NameFixed = NameInPascalCase(entity.Name);
                foreach (var property in entityType.Descendants().Where(n => n.Name.LocalName == "Property"))
                {
                    Property prop = new Property
                    {
                        Name = property.Attribute("Name")?.Value ?? string.Empty,
                        Type = property.Attribute("Type")?.Value ?? string.Empty,
                        IsNullable = !(property.Attribute("Nullable")?.Value == "false"),
                        Precision = Convert.ToInt32(property.Attribute("Precision")?.Value ?? "0"),
                        Scale = Convert.ToInt32(property.Attribute("Scale")?.Value ?? "0"),
                        FixedLength = property.Attribute("FixedLength")?.Value == "true",
                        Unicode = property.Attribute("Unicode")?.Value == "true"
                    };
                    string maxLength = property.Attribute("MaxLength")?.Value ?? string.Empty;
                    prop.MaxLength = maxLength == string.Empty ? null : maxLength == "Max" ? -1 : Convert.ToInt32(maxLength ?? "0");
                    prop.IsPrimaryKey = entityType.Descendants().Any(n => n.Name.LocalName == "Key" && n.Descendants().Any(k => k.Attribute("Name")?.Value == prop.Name));
                    entity.Properties.Add(prop);
                }
                data[entity.Name] = entity;
            }
            return data;
        }
        /// <summary>
        /// C-S mapping content
        /// </summary>
        /// <returns>Tables list</returns>
        private IList<Mapping> GetMappings(XElement? edmxRuntime)
        {
            IList<Mapping> data = new List<Mapping>();
            XElement? mappingsRootElement = edmxRuntime?.Descendants().FirstOrDefault(n => n.Name.LocalName == "Mappings");
            XElement? mappingsElement = mappingsRootElement?.Descendants().FirstOrDefault(n => n.Name.LocalName == "Mapping");
            XElement? context = mappingsElement?.Descendants().FirstOrDefault(n => n.Name.LocalName == "EntityContainerMapping");
            if (context == null)
            {
                throw new InvalidOperationException("The EDMX file does not contain any mappings.");
            }
            string storageContext = context.Attribute("StorageEntityContainer")?.Value ?? string.Empty;
            string entityContext = context.Attribute("CdmEntityContainer")?.Value ?? string.Empty;
            foreach (XElement? mappingElement in context.Descendants().Where(n => n.Name.LocalName == "EntitySetMapping"))
            {
                string name = mappingElement.Attribute("Name")?.Value ?? string.Empty;
                XElement? storeEntitySet = mappingElement.Descendants().FirstOrDefault(n => n.Name.LocalName == "EntityTypeMapping");
                XElement? fragment = mappingElement.Descendants().FirstOrDefault(n => n.Name.LocalName == "MappingFragment");

                Mapping mapping = new Mapping
                {
                    TableName = fragment.Attribute("StoreEntitySet")?.Value ?? string.Empty,
                    EntityName = (storeEntitySet.Attribute("TypeName")?.Value ?? string.Empty).Replace("BDTMS.", string.Empty)
                };
                if (mapping.EntityName == "Regla1")
                {
                    mapping.EntityName = "REGLA";
                }
                else if (mapping.EntityName == "REGLA")
                {
                    mapping.EntityName = "Regla1";
                }
                foreach (XElement? scalarProperty in mappingElement.Descendants().Where(n => n.Name.LocalName == "ScalarProperty"))
                {
                    MappingProperty mappingProperty = new MappingProperty
                    {
                        ColumnName = scalarProperty.Attribute("ColumnName")?.Value ?? string.Empty,
                        PropertyName = scalarProperty.Attribute("Name")?.Value ?? string.Empty
                    };
                    mapping.Properties.Add(mappingProperty);
                }
                data.Add(mapping);
            }
            return data;
        }
        private void bkWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            txtLog.BeginInvoke(new Action(() =>
            {
                bntFileSelection.Enabled = true;
                btnFindDestination.Enabled = true;
                btnRun.Enabled = true;
            }));
        }

    }
    public class UIParameters
    {
        public string FilePath { get; set; }
        public string DestinationPath { get; set; }
        public string Namespace { get; set; }
        public bool CreateDbContext { get; internal set; }
        public bool CreateModels { get; internal set; }
        public bool CreateRepositories { get; internal set; }
        public bool CreateConfigurations { get; internal set; }
    }
}
