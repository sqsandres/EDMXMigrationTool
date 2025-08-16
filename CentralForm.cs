using System.Text;
using System.Xml.Linq;

namespace EDMXMigrationTool
{
    public partial class frmCentralForm : Form
    {
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
                Namespace = txtNamespace.Text
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
                UIParameters parameters = (UIParameters)e.Argument;
                Dictionary<string, byte> schemas = new Dictionary<string, byte>();
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
                AddLog("Creating configuration classes:");
                CreateFolder(Path.Combine(parameters.DestinationPath, "Configuration"));
                CreateConfigurationClasses(tables, entities, mappings, parameters);
                AddLog("Creating models classes:");
                CreateFolder(Path.Combine(parameters.DestinationPath, "Models"));
                CreateModelClasses(tables, entities, mappings, parameters);
                AddLog("Creating the DBContext:");
                CreateDbContext(entities, parameters);
                AddLog("Migrated!");
            }
            catch (Exception ex)
            {
                AddLog($"Error during migration: {ex.Message}");
            }
        }
        private void CreateModelClasses(IDictionary<string, Table> tables, IDictionary<string, Entity> entities, IList<Mapping> mappings, UIParameters parameters)
        {
            foreach (Entity entity in entities.Values)
            {
                StringBuilder file = new StringBuilder();
                file.AppendLine("using System;");
                file.Append(Environment.NewLine);
                file.Append("namespace ");
                file.Append(parameters.Namespace);
                file.Append(".Models");
                file.AppendLine(" {");
                file.Append("   public class ");
                file.Append(entity.NameFixed);
                file.AppendLine(" {");
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
                            break;
                        case "Int64":
                            file.Append("long");
                            break;
                        case "Boolean":
                            file.Append("bool");
                            break;
                        case "DateTime":
                            file.Append("DateTime");
                            break;
                        case "Decimal":
                            file.Append("decimal");
                            break;
                        case "Double":
                            file.Append("double");
                            break;
                        case "Guid":
                            file.Append("Guid");
                            break;
                        case "Byte[]":
                            file.Append("byte[]");
                            break;
                        case "Byte":
                            file.Append("byte");
                            break;
                        case "Int16":
                            file.Append("short");
                            break;
                        case "Binary":
                            file.Append("byte[]");
                            break;
                        default:
                            throw new Exception($"Unsupported type: {property.Type}");
                    }
                    if (!property.IsNullable)
                    {
                        file.Append("?");
                    }
                    file.Append(" ");
                    file.Append(property.Name);
                    file.AppendLine("  { get; set; }");
                }
                file.AppendLine("       public override string ToString(){");
                file.Append("           return $\"");
                foreach (Property property in entity.Properties)
                {
                    file.Append("{");
                    file.Append(property.Name);
                    if (property.IsNullable)
                    {
                        file.Append("?.ToString()");
                    }
                    file.Append("} - ");
                }
                file.AppendLine("\";");
                file.AppendLine("       }");
                file.AppendLine("   }");
                file.Append("}");
                File.WriteAllText(Path.Combine(parameters.DestinationPath, "Models", entity.NameFixed + ".cs"), file.ToString());
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
            foreach (Table table in tables.Values)
            {
                bool hasSchema = !string.IsNullOrEmpty(table.Schema) && !table.Schema.Equals("dbo");
                StringBuilder file = new StringBuilder();
                file.AppendLine("using System;");
                file.AppendLine("using Microsoft.EntityFrameworkCore;");
                file.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
                if (hasSchema)
                {
                    file.Append("using ");
                    file.Append(parameters.Namespace);
                    file.Append(".Models.");
                    file.Append(table.Schema);
                    file.AppendLine(";");
                }
                file.Append(Environment.NewLine);
                file.Append("namespace ");
                file.Append(parameters.Namespace);
                file.Append(".Configuration");
                if (hasSchema)
                {
                    file.Append(".");
                    file.Append(table.Schema);
                }
                file.AppendLine(" {");
                file.Append("   public class ");
                file.Append(table.NameFixed);
                file.Append("Configuration : IEntityTypeConfiguration<");
                file.Append(table.NameFixed);
                file.AppendLine(">{");
                file.Append("       public void Configure(EntityTypeBuilder<");
                file.Append(table.NameFixed);
                file.AppendLine("> builder){");
                file.Append("           builder.ToTable(\"");
                file.Append(table.Name);
                file.Append("\", SchemaName.");
                file.Append(table.Schema);
                file.AppendLine(");");
                foreach (Column column in table.Columns)
                {
                    if (column.IsPrimaryKey)
                    {
                        file.Append("           builder.HasKey(x => x.");
                        file.Append(column.Name);
                        file.AppendLine(");");
                    }
                    file.Append("           builder.Property(x => x.");
                    file.Append(")");
                    if (!column.IsNullable)
                    {
                        file.Append(".IsRequired()");
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
                        file.AppendLine(")");
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

                file.Append("       }");
                file.Append("   }");
                file.Append("}");
                File.WriteAllText(Path.Combine(parameters.DestinationPath, "Configuration", table.NameFixed + ".cs"), file.ToString());
            }
        }
        private void CreateDbContext(IDictionary<string, Entity> entities, UIParameters parameters)
        {
            StringBuilder file = new StringBuilder();
            file.AppendLine("using System;");
            file.AppendLine("using System.Collections.Generic;");
            file.AppendLine("using System.Data;");
            file.AppendLine("using System.Data.Common;");
            file.AppendLine("using System.Linq;");
            file.AppendLine("using System.Threading;");
            file.AppendLine("using System.Threading.Tasks;");
            file.AppendLine("using Microsoft.EntityFrameworkCore;");
            file.AppendLine("using Microsoft.EntityFrameworkCore.ChangeTracking;");
            file.AppendLine("using Microsoft.Extensions.Logging;");
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
            foreach (Entity entity in entities.Values.OrderBy(x => x.Name))
            {
                file.Append("       public DbSet<");
                file.Append(entity.NameFixed);
                file.Append("> ");
                file.Append(entity.NameFixed);
                file.AppendLine("s { get; set; }");
            }
            file.Append(Environment.NewLine);
            file.AppendLine("       protected override void OnModelCreating(ModelBuilder modelBuilder){");
            file.AppendLine("           base.OnModelCreating(modelBuilder);");
            foreach (Entity entity in entities.Values.OrderBy(x => x.Name))
            {
                file.Append("           modelBuilder.ApplyConfiguration(new ");
                file.Append(entity.NameFixed);
                file.AppendLine("Configuration());");
            }
            file.AppendLine("       }");
            file.AppendLine("   }");
            file.Append("}");
            File.WriteAllText(Path.Combine(parameters.DestinationPath, "AppDbContext.cs"), file.ToString());
        }
        private void AnaliceContext(IDictionary<string, Table> tables, IDictionary<string, Entity> entities, IList<Mapping> mappings)
        {

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
            string schemaName = schema?.Attribute("Namespace")?.Value ?? string.Empty;
            foreach (var entity in schema.Descendants())
            {
                if (entity.Name.LocalName == "EntityType")
                {
                    Table table = new Table
                    {
                        Name = entity.Attribute("Name")?.Value ?? string.Empty,
                        Schema = schemaName ?? string.Empty
                    };
                    table.NameFixed = FixedTableName(table.Name);
                    foreach (var property in entity.Descendants().Where(n => n.Name.LocalName == "Property"))
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
                        table.Columns.Add(column);
                    }
                    data[table.Name] = table;
                }
            }
            return data;
        }
        private string FixedTableName(string name)
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
            return pascalName.ToString();
        }
        private bool IsInPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            return char.IsUpper(name[0]) && (char.IsLower(name[1]) || char.IsLower(name[5]));
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
            string schemaName = schema?.Attribute("Namespace")?.Value ?? string.Empty;
            foreach (var entityType in schema.Descendants().Where(n => n.Name.LocalName == "EntityType"))
            {
                Entity e = new Entity
                {
                    Name = entityType.Attribute("Name")?.Value ?? string.Empty,
                    TableName = entityType.Attribute("Name")?.Value ?? string.Empty,
                    Schema = schemaName ?? string.Empty
                };
                e.NameFixed = FixedTableName(e.Name);
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
                    e.Properties.Add(prop);
                }
                data[e.Name] = e;
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
                    EntityName = mappingElement.Attribute("Name")?.Value ?? string.Empty
                };
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
    }
}
