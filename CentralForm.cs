using System.Text;
using System.Xml.Linq;

namespace EDMXMigrationTool
{
    public partial class frmCentralForm : Form
    {
        private const string DefaultSchema = "General";
        private XNamespace StoreNameSpace = "http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator";
        public frmCentralForm()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Handles the click event for the file selection button, allowing the user to choose an EDMX file and updates
        /// the file path display.
        /// </summary>
        /// <remarks>The file dialog filters for files with the .edmx extension. If the user selects a
        /// file and confirms, the selected file path is displayed in the associated text box.</remarks>
        /// <param name="sender">The source of the event, typically the file selection button.</param>
        /// <param name="e">An EventArgs instance containing event data.</param>
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
        /// <summary>
        /// Handles the Click event for the Find Destination button, allowing the user to select a destination folder
        /// for migration files.
        /// </summary>
        /// <remarks>Displays a folder browser dialog for the user to choose a destination folder. If a
        /// folder is selected, the path is set in the destination text box.</remarks>
        /// <param name="sender">The source of the event, typically the Find Destination button.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        public void btnFindDestination_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Select the destination folder for the migration files";
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtDestination.Text = folderBrowserDialog.SelectedPath;
            }
        }
        /// <summary>
        /// Handles the Click event of the Run button, validating user input and initiating the background process to
        /// generate files based on the selected EDMX file and destination.
        /// </summary>
        /// <remarks>If either the EDMX file path or the destination folder is not specified, an error
        /// message is displayed and the operation is aborted. When valid input is provided, the method disables related
        /// controls and starts the background worker with the specified parameters.</remarks>
        /// <param name="sender">The source of the event, typically the Run button control.</param>
        /// <param name="e">An EventArgs instance containing event data.</param>
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
                CreateConfigurations = chkCreateConfigurations.Checked,
                DbName = txtDbName.Text
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
        /// <summary>
        /// Handles the background migration process when the associated BackgroundWorker raises the DoWork event.
        /// Loads, parses, and migrates the specified EDMX file, generates related classes and folders, and logs
        /// progress and errors throughout the operation.
        /// </summary>
        /// <remarks>This method performs a series of migration steps, including reading the EDMX file,
        /// analyzing entities and mappings, creating necessary folders, and generating configuration, model, context,
        /// and repository classes based on the provided parameters. Progress and errors are logged for monitoring
        /// purposes. If the EDMX file is missing or invalid, the method logs the error and terminates the migration
        /// process.</remarks>
        /// <param name="sender">The source of the event, typically the BackgroundWorker instance that triggered the DoWork event.</param>
        /// <param name="e">A DoWorkEventArgs object containing the event data, including the migration parameters supplied via the
        /// Argument property.</param>
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
                string edmxContent = File.ReadAllText(edmxFilePath);
                //2. Parse the EDMX file
                AddLog("Parsing EDMX file...");
                XDocument edmxDocument = XDocument.Parse(edmxContent);
                //3. Migrate the EDMX file
                AddLog("Migrating EDMX file...");
                //4. Listing all entities and their properties
                XElement? edmxRuntime = edmxDocument?.Descendants().FirstOrDefault(n => n.Name.LocalName == "Runtime");
                if (edmxRuntime == null)
                {
                    throw new InvalidOperationException("The EDMX file does not contain any entities.");
                }
                AddLog(" --> Reading Tables:");
                StorageModel storageModels = GetStorageModels(edmxRuntime);
                AddLog($" --> Tables:{storageModels.Tables.Count}");
                AddLog(" --> Reading Entities:");
                ConceptualModel conceptualModel = GetConceptualModels(edmxRuntime);
                AddLog($" --> Entities:{conceptualModel.Entities.Count}");
                AddLog(" --> Reading mappings:");
                IList<Mapping> mappings = GetMappings(edmxRuntime);
                AddLog($" --> Mappings:{mappings.Count}");
                AddLog("Analysing:");
                AnaliceContext(storageModels.Tables, conceptualModel.Entities, mappings);
                AddLog("Creating the schema enum:");
                List<string> schemas = CreateSchemaClass(storageModels, parameters);

                AddLog("Creating folders:");
                CreateFolder(Path.Combine(parameters.DestinationPath, "Configuration"));
                CreateFolder(Path.Combine(parameters.DestinationPath, "Domain"));
                CreateFolder(Path.Combine(parameters.DestinationPath, "IRepositories"));
                CreateFolder(Path.Combine(parameters.DestinationPath, "Repositories"));
                foreach (string? schema in schemas.Where(x => x != "dbo"))
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
                    CreateConfigurationClasses(storageModels, conceptualModel, mappings, parameters);
                }
                if (parameters.CreateModels)
                {
                    AddLog("Creating models classes:");
                    CreateModelClasses(storageModels, conceptualModel, mappings, parameters);
                }
                if (parameters.CreateDbContext)
                {
                    AddLog("Creating the DBContext with dbSets:");
                    CreateDbContext(storageModels, conceptualModel, parameters, "DbContext" + parameters.DbName, true, schemas);
                    //AddLog("Creating the DBContext without dbSets:");
                    //CreateDbContext(entities, parameters, "AppDbContext", false, schemas);
                }
                if (parameters.CreateRepositories)
                {
                    AddLog("Creating repositories classes:");
                    CreateRepositoriesClasses(storageModels.Tables, conceptualModel.Entities, mappings, parameters);
                }
                AddLog("Migrated!");
            }
            catch (Exception ex)
            {
                AddLog($"Error during migration: {ex.Message}");
            }
        }
        private List<string> CreateSchemaClass(StorageModel storageModel, UIParameters parameters)
        {
            StringBuilder file = new StringBuilder();
            file.AppendLine("using System;");
            file.Append(Environment.NewLine);
            file.Append("namespace ");
            file.Append(parameters.Namespace);
            file.AppendLine("    /// <summary>");
            file.AppendLine("    /// Provides constant string values for commonly used database schema names within the application.");
            file.AppendLine("    /// </summary>");
            file.AppendLine("    /// <remarks>Use these constants to reference database schemas in a consistent and type-safe manner");
            file.AppendLine("    /// throughout the codebase. This helps prevent errors caused by misspelled schema names and improves");
            file.AppendLine("    /// maintainability when schema names change.</remarks>");
            file.Append("   public class SchemaName");
            file.AppendLine(parameters.DbName);
            file.AppendLine("    {");
            List<string> schemas = storageModel.Tables.Values.Select(x => x.Schema)
                                   .Union(storageModel.Functions.Values.Where(t => HasSchema(t.Schema))
                                .Select(t => t.Schema)).Distinct().ToList();
            foreach (string schema in schemas)
            {
                file.AppendLine("        /// <summary>");
                file.Append("        /// Represents the constant string value \"");
                file.Append(schema);
                file.AppendLine("\".");
                file.AppendLine("        /// </summary>");
                file.Append("       public const string ");
                if (schema.Equals("dbo", StringComparison.InvariantCultureIgnoreCase))
                {
                    file.AppendLine("General = \"dbo\";");
                }
                else
                {
                    file.Append(NameInPascalCase(schema, false));
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
        /// <summary>
        /// Determines whether the specified schema name represents a non-default schema.
        /// </summary>
        /// <remarks>Use this method to check if a schema name refers to a custom or non-default schema,
        /// as "dbo" is typically the default schema in SQL Server.</remarks>
        /// <param name="name">The name of the schema to evaluate. Cannot be null or empty.</param>
        /// <returns>true if the specified schema name is not null, not empty, and not equal to "dbo"; otherwise, false.</returns>
        private bool HasSchema(string name)
        {
            return !string.IsNullOrEmpty(name) && !name.Equals("dbo");
        }
        /// <summary>
        /// Generates repository interface and implementation classes for each used entity and writes them to the
        /// specified destination paths.
        /// </summary>
        /// <remarks>This method creates both interface and concrete repository classes for each entity
        /// that is marked as used. The generated files are organized by schema and written to the appropriate
        /// directories based on the provided parameters. Existing files may be overwritten if they already exist at the
        /// destination path.</remarks>
        /// <param name="tables">A dictionary containing table definitions, keyed by table name. Used to provide schema information for
        /// repository generation.</param>
        /// <param name="entities">A dictionary containing entity definitions, keyed by entity name. Only entities marked as used are processed
        /// to generate repositories.</param>
        /// <param name="mappings">A list of mapping objects that define relationships between tables and entities. Used to inform repository
        /// structure.</param>
        /// <param name="parameters">An object containing UI and configuration parameters, including namespace and destination paths for
        /// generated files.</param>
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
                repoFile.AppendLine($"   public class {entity.NameFixed}Repository : Repositorio<Domain.{(hasSchema ? entity.Schema : DefaultSchema)}.{entity.NameFixed}>, I{entity.NameFixed}Repository {{");
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
        /// <summary>
        /// Generates C# model class files for each used entity in the conceptual model, including properties and
        /// navigation properties, and writes them to the specified destination path.
        /// </summary>
        /// <remarks>Each generated class includes XML documentation comments for its members, and
        /// navigation properties are created for entity relationships. The method writes output files to
        /// schema-specific subfolders under the destination path. The ToString method in each class provides a
        /// formatted summary of key property values for debugging or logging purposes.</remarks>
        /// <param name="storage">The storage model containing database schema information used for mapping entities and properties.</param>
        /// <param name="conceptualModel">The conceptual model that defines the entities and their relationships to be represented as C# classes.</param>
        /// <param name="mappings">A list of mapping objects that specify how conceptual entities and properties correspond to storage schema
        /// elements.</param>
        /// <param name="parameters">The UI parameters that provide configuration options such as namespace, database name, and destination path
        /// for generated files.</param>
        /// <exception cref="Exception">Thrown if an entity property type is unsupported during class generation.</exception>
        private void CreateModelClasses(StorageModel storage, ConceptualModel conceptualModel, IList<Mapping> mappings, UIParameters parameters)
        {
            foreach (Entity entity in conceptualModel.Entities.Values.Where(x => x.Used))
            {
                bool hasSchema = HasSchema(entity.Schema);
                StringBuilder file = new StringBuilder();
                file.AppendLine("using System;");
                file.Append(Environment.NewLine);
                file.Append("namespace ");
                file.Append(parameters.Namespace);
                file.Append(".Domain.");
                file.Append(parameters.DbName);
                file.Append(".");
                if (hasSchema)
                {
                    file.Append(entity.Schema);
                }
                else
                {
                    file.Append(DefaultSchema);
                }
                file.AppendLine(" {");
                file.AppendLine("    /// <summary>");
                file.AppendLine("    /// Represents entity.");
                file.AppendLine("    /// </summary>");

                file.Append("    public class ");
                file.Append(entity.NameFixed);
                file.AppendLine(" : Entity {");
                file.AppendLine("        /// <summary>");
                file.AppendLine("        /// Initializes a new instance of this class.");
                file.AppendLine("        /// </summary>");
                file.Append("        public ");
                file.Append(entity.NameFixed);
                file.AppendLine("() { }");
                file.Append(Environment.NewLine);
                foreach (Property property in entity.Properties)
                {
                    file.AppendLine("        /// <summary>");
                    file.Append("        /// Gets or sets ");
                    file.AppendLine(property.Name);
                    file.AppendLine("        /// </summary>");
                    file.Append("        public ");
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
                        case "Single":
                            file.Append("Single");
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
                        case "Time":
                            file.Append("TimeSpan");
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
                file.AppendLine(string.Empty);
                if (entity.NavigationProperties.Count > 0)
                {
                    file.AppendLine("        //-------------------------------------");
                    file.AppendLine("        // Navigation properties");
                    file.AppendLine("        //-------------------------------------");
                    foreach (NavigationProperty nProp in entity.NavigationProperties)
                    {
                        file.AppendLine("        /// <summary>");
                        file.Append("        /// Gets or sets ");
                        file.AppendLine(nProp.TargetName);
                        file.AppendLine("        /// </summary>");
                        file.Append("        public ");
                        if (nProp.Multiplicity == Multiplicity.Many || nProp.Multiplicity == Multiplicity.ZeroOrMany)
                        {
                            file.Append("ICollection<Domain.");
                            file.Append(parameters.DbName);
                            file.Append(".");
                            file.Append(nProp.TargetSchema == "dbo" ? DefaultSchema : nProp.TargetSchema);
                            file.Append(".");
                            file.Append(nProp.TargetNameFixed);
                            file.Append("> ");
                            file.Append(nProp.TargetNameFixedWithCounter);
                        }
                        else
                        {
                            file.Append("Domain.");
                            file.Append(parameters.DbName);
                            file.Append(".");
                            file.Append(nProp.TargetSchema == "dbo" ? DefaultSchema : nProp.TargetSchema);
                            file.Append(".");
                            file.Append(nProp.TargetNameFixed);
                            file.Append(" ");
                            file.Append(nProp.TargetNameFixedWithCounter);
                        }
                        file.AppendLine(" { get; set; }");
                    }
                }
                file.AppendLine("        /// <summary>");
                file.AppendLine("        /// Returns a string that represents the current object, including key property values in a formatted sequence.");
                file.AppendLine("        /// </summary>");
                file.AppendLine("        /// <remarks>The returned string provides a concise summary of the object's state, which can be");
                file.AppendLine("        /// useful for logging or debugging purposes.</remarks>");
                file.Append("        /// <returns>A string containing the values of:");
                file.AppendLine(string.Join(", ", entity.Properties.Select(p => p.Name)));
                file.AppendLine("        /// separated by hyphens.</returns>");
                file.AppendLine("        public override string ToString(){");
                file.Append("            return $\"");
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
        /// <summary>
        /// Creates a new folder at the specified path if it does not already exist.
        /// </summary>
        /// <remarks>If the folder already exists at the specified path, no action is taken. An exception
        /// is thrown if the path is invalid or if the folder cannot be created due to permission issues or other I/O
        /// errors.</remarks>
        /// <param name="path">The file system path where the folder should be created. Cannot be null, empty, or contain invalid path
        /// characters.</param>
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
        /// <summary>
        /// Generates Entity Framework Core configuration classes for each mapped entity and table, writing them to the
        /// appropriate destination path.
        /// </summary>
        /// <remarks>This method creates configuration classes that define table mappings, primary keys,
        /// property requirements, and foreign key relationships for use with Entity Framework Core. The generated
        /// classes are intended to be used within the DbContext's OnModelCreating method to apply entity
        /// configurations. Each configuration file is written to a path based on the schema and entity name. The method
        /// is not intended to be called directly from application code.</remarks>
        /// <param name="tables">A dictionary containing table definitions, keyed by table name. Each entry provides schema and column
        /// information required for configuration.</param>
        /// <param name="entities">A dictionary containing entity definitions, keyed by entity name. Each entry represents a domain model to be
        /// mapped to a table.</param>
        /// <param name="mappings">A list of mapping objects that define the relationships between entities and tables, including property and
        /// key mappings.</param>
        /// <param name="parameters">The UI parameters specifying namespace, database name, and destination path for the generated configuration
        /// classes.</param>
        /// <exception cref="Exception">Thrown if an unsupported foreign key multiplicity combination is encountered during configuration
        /// generation.</exception>
        private void CreateConfigurationClasses(StorageModel storage, ConceptualModel conceptualModel, IList<Mapping> mappings, UIParameters parameters)
        {
            foreach (Mapping mapping in mappings)
            {
                Table table = storage.Tables[mapping.TableName];
                Entity entity = conceptualModel.Entities[mapping.EntityName];
                bool hasSchema = HasSchema(table.Schema);
                StringBuilder file = new StringBuilder();
                file.AppendLine("using Microsoft.EntityFrameworkCore;");
                file.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
                file.Append(Environment.NewLine);
                file.Append("namespace ");
                file.Append(parameters.Namespace);
                file.Append(".Configuration.");
                file.Append(parameters.DbName);
                file.Append(".");
                file.Append(hasSchema ? table.Schema : DefaultSchema);
                file.Append(Environment.NewLine);
                file.AppendLine("{");
                file.AppendLine("    /// <summary>");
                file.Append("    /// Provides the Entity Framework Core configuration for the ");
                file.Append(table.Name);
                file.AppendLine("table.");
                file.AppendLine("    /// </summary>");
                file.AppendLine("    /// <remarks>This configuration defines the table mapping, primary key, property requirements, and");
                file.Append("    /// constraints for the ");
                file.Append(table.Name);
                file.Append(" table within the ");
                file.Append(table.Schema);
                file.AppendLine(" schema. It is typically used by the");
                file.AppendLine("    /// Entity Framework Core infrastructure and is not intended to be used directly in application code.</remarks>");
                file.Append("    public class ");
                file.Append(table.EntityName);
                file.Append("Configuration : IEntityTypeConfiguration<Domain.");
                file.Append(parameters.DbName);
                file.Append(".");
                file.Append(hasSchema ? table.Schema : DefaultSchema);
                file.Append(".");
                file.Append(table.EntityName);
                file.AppendLine(">");
                file.AppendLine("{");
                file.AppendLine("        /// <summary>");
                file.Append("        /// Configures the entity type mapping for the ");
                file.Append(table.NameFixed);
                file.AppendLine(" domain model.");
                file.AppendLine("        /// </summary>");
                file.AppendLine("        /// <remarks>Call this method within the OnModelCreating method of your DbContext to apply entity");
                file.AppendLine("        /// configuration for this entity. This includes table mapping, property requirements, maximum");
                file.AppendLine("        /// lengths, and key definitions.</remarks>");
                file.Append("        /// <param name=\"builder\" > The builder used to configure the ");
                file.Append(table.NameFixed);
                file.AppendLine(" entity type.</param>");
                file.Append("        public void Configure(EntityTypeBuilder<Domain.");
                file.Append(parameters.DbName);
                file.Append(".");
                file.Append(hasSchema ? table.Schema : DefaultSchema);
                file.Append(".");
                file.Append(table.EntityName);
                file.AppendLine("> builder)");
                file.AppendLine("        {");
                if (table.RepoType == RepoType.View)
                {
                    file.AppendLine("           builder.HasNoKey();");
                    file.Append("           builder.ToView(\"");
                }
                else
                {
                    file.Append("           builder.ToTable(\"");
                }
                file.Append(table.Name);
                file.Append("\", SchemaName");
                file.Append(parameters.DbName);
                file.Append(".");
                file.Append(string.Equals(table.Schema, "dbo", StringComparison.InvariantCultureIgnoreCase)
                    ? "General"
                    : table.Schema);
                file.AppendLine(");");
                if (table.RepoType == RepoType.Table)
                {
                    List<Column> primaryKeys = table.Columns.Values.Where(c => c.IsPrimaryKey).ToList();
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
                    if (prop.PropertyName != prop.ColumnName)
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
                if (table.ForeingKeys.Count > 0)
                {
                    foreach (ForeingKey fk in table.ForeingKeys)
                    {
                        file.Append("           ");
                        // many to one
                        if (
                            fk.Destination is Multiplicity.Many or Multiplicity.ZeroOrMany &&
                            fk.Source is Multiplicity.One or Multiplicity.ZeroOrOne
                        )
                        {
                            file.Append($"builder.HasMany(x => x.{fk.TableNameFixedWithCounter}).WithOne(x => x.{table.NameFixed}).HasForeignKey(x => ");
                            if (fk.Columns.Count == 1)
                            {
                                file.AppendLine($"x.{fk.Columns[0].Value});");
                            }
                            else
                            {
                                file.Append("new {");
                                file.Append(string.Join(", ", fk.Columns.Select(c => "x." + c.Value)));
                                file.AppendLine("});");
                            }
                        }
                        else
                        /* one to many */
                        if (
                            fk.Destination is Multiplicity.One or Multiplicity.ZeroOrOne &&
                            fk.Source is Multiplicity.Many or Multiplicity.ZeroOrMany
                        )
                        {
                            file.Append($"builder.HasOne(x => x.{fk.TableNameFixedWithCounter}).WithMany(x => x.{table.NameFixed}).HasForeignKey(x => ");
                            if (fk.Columns.Count == 1)
                            {
                                file.AppendLine($"x.{fk.Columns[0].Key});");
                            }
                            else
                            {
                                file.Append("new {");
                                file.Append(string.Join(", ", fk.Columns.Select(c => "x." + c.Key)));
                                file.AppendLine("});");
                            }
                        }
                        else
                        /* one to one */
                        if (
                            (fk.Destination == Multiplicity.One || fk.Destination == Multiplicity.ZeroOrOne) &&
                            (fk.Source == Multiplicity.One || fk.Source == Multiplicity.ZeroOrOne)
                        )
                        {
                            file.Append($"builder.HasOne(x => x.{fk.TableNameFixedWithCounter}).WithOne(x => x.{table.NameFixed}).HasForeignKey<Domain.{parameters.DbName}.{(fk.TableSchema == "dbo" ? DefaultSchema : fk.TableSchema)}.{fk.TableNameFixed}>(x => ");
                            if (fk.Columns.Count == 1)
                            {
                                file.AppendLine($"x.{fk.Columns[0].Value});");
                            }
                            else
                            {
                                file.Append("new {");
                                file.Append(string.Join(", ", fk.Columns.Select(c => "x." + c.Value)));
                                file.AppendLine("});");
                            }
                        }
                        else
                        {
                            throw new Exception("WFT!!");
                        }
                    }
                }
                file.AppendLine("       }");
                file.AppendLine("   }");
                file.AppendLine("}");
                File.WriteAllText(Path.Combine(parameters.DestinationPath, "Configuration",
                    (hasSchema ? table.Schema : DefaultSchema), table.EntityName + "Configuration.cs")
                    , file.ToString());
            }



            foreach (Function fn in conceptualModel.Functions.Where(x => !x.Value.IsFunction && !string.IsNullOrEmpty(x.Value.ReturnComplexType) && x.Value.ReturnComplexType.Contains("Collection")).Select(x => x.Value))
            {
                StorageFunction sfn = storage.Functions[fn.Name];
                bool hasSchema = HasSchema(sfn.Schema);
                StringBuilder file = new StringBuilder();
                string returnType = (fn.ReturnComplexType?.Replace("Collection(", string.Empty).Replace(")", string.Empty) ?? string.Empty).Replace("_", string.Empty);
                file.AppendLine("using Microsoft.EntityFrameworkCore;");
                file.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
                file.Append(Environment.NewLine);
                file.Append("namespace ");
                file.Append(parameters.Namespace);
                file.Append(".Configuration.");
                file.Append(parameters.DbName);
                file.Append(".");
                file.Append(hasSchema ? sfn.Schema : DefaultSchema);
                file.Append(Environment.NewLine);
                file.AppendLine("{");
                file.AppendLine("    /// <summary>");
                file.AppendLine("    /// ");
                file.AppendLine("    /// </summary>");
                file.Append("    public class Procedure");
                file.Append(returnType);
                file.Append("Configuration : IEntityTypeConfiguration<Domain.");
                file.Append(parameters.DbName);
                file.Append(".");
                file.Append(hasSchema ? sfn.Schema : DefaultSchema);
                file.Append(".");
                file.Append(fn.NameFixed);
                file.AppendLine(">");
                file.AppendLine("{");
                file.AppendLine("        /// <summary>");
                file.Append("        ///");
                file.AppendLine("        /// </summary>");
                file.Append("        public void Configure(EntityTypeBuilder<Domain.");
                file.Append(parameters.DbName);
                file.Append(".");
                file.Append(hasSchema ? sfn.Schema : DefaultSchema);
                file.Append(".");
                file.Append(returnType);
                file.AppendLine("> builder)");
                file.AppendLine("        {");
                file.AppendLine("           builder.HasNoKey();");
                foreach (Column column in sfn.ReturnColumns)
                {
                    file.Append("           builder.Property(x => x.");
                    file.Append(column.PropertyName);
                    file.Append(")");
                    if (!column.IsNullable)
                    {
                        file.Append(".IsRequired()");
                    }
                    file.Append(".HasColumnName(\"");
                    file.Append(column.Name);
                    file.Append("\")");
                }
                file.AppendLine("       }");
                file.AppendLine("   }");
                file.AppendLine("}");
                File.WriteAllText(Path.Combine(parameters.DestinationPath, "Configuration",
                        (hasSchema ? sfn.Schema : DefaultSchema), $"Procedure{sfn.NameFixed}Configuration.cs")
                    , file.ToString());
            }
        }
        /// <summary>
        /// Generates the Entity Framework Core database context source code file for the specified data model and
        /// configuration parameters.
        /// </summary>
        /// <remarks>This method creates a C# source file that defines a strongly-typed Entity Framework
        /// Core DbContext class, including entity sets, configuration, and optional function mappings. The generated
        /// context is tailored to the provided models and parameters, and should be integrated into the application's
        /// data access layer. Ensure that all required models and configuration options are supplied to avoid
        /// incomplete or invalid context generation.</remarks>
        /// <param name="storageModel">The storage model representing the database schema and physical structure to be used for context generation.
        /// Cannot be null.</param>
        /// <param name="conceptualModel">The conceptual model containing the application's entity definitions and relationships. Must include all
        /// entities to be exposed in the context.</param>
        /// <param name="parameters">The UI and configuration parameters that specify code generation options, such as namespace, destination
        /// path, and database name. Cannot be null.</param>
        /// <param name="name">The name of the generated context source file, excluding the file extension. Must be a valid file name.</param>
        /// <param name="hasDbSets">A value indicating whether DbSet properties should be generated for the entities in the context. If <see
        /// langword="true"/>, DbSet properties are included; otherwise, they are omitted.</param>
        /// <param name="schemas">A list of database schema names to be included in the generated context. Schemas other than 'dbo' will have
        /// separate configuration imports.</param>
        private void CreateDbContext(StorageModel storageModel, ConceptualModel conceptualModel, UIParameters parameters, string name, bool hasDbSets, List<string> schemas)
        {
            StringBuilder file = new StringBuilder();
            file.AppendLine("using Microsoft.EntityFrameworkCore;");
            file.Append(Environment.NewLine);
            file.Append("namespace ");
            file.Append(parameters.Namespace);
            file.AppendLine("{");
            file.AppendLine("    /// <summary>");
            file.AppendLine("    /// Represents the Entity Framework Core database context for the application, providing access to entity sets and");
            file.AppendLine("    /// configuration for the underlying database schema.");
            file.AppendLine("    /// </summary>");
            file.AppendLine("    /// <remarks>AppDbContext manages the application's data model and is used to query and save instances of");
            file.AppendLine("    /// entity types. It exposes DbSet properties for each entity in the domain, enabling LINQ queries and change");
            file.AppendLine("    /// tracking. This context should be registered and managed according to the application's dependency injection and");
            file.AppendLine("    /// lifetime requirements. For advanced configuration, override OnModelCreating or use the provided options in the");
            file.AppendLine("    /// constructor.</remarks>");
            file.Append("    public class DbContext");
            file.Append(parameters.DbName);
            file.AppendLine(" : DbContext{");
            file.AppendLine("        /// <summary>");
            file.AppendLine("        /// Initializes a new instance of the DbContext class using the specified options.");
            file.AppendLine("        /// </summary>");
            file.AppendLine("        /// <param name=\"options\">The options to be used by the DbContext. Must not be null.</param>");
            file.Append("        public DbContext");
            file.Append(parameters.DbName);
            file.Append("(DbContextOptions<DbContext");
            file.Append(parameters.DbName);
            file.Append("> options) : base(options){");
            file.AppendLine("        }");
            file.AppendLine("        /// <summary>");
            file.AppendLine("        /// Configures the database context options for this context instance.");
            file.AppendLine("        /// </summary>");
            file.AppendLine("        /// <param name=\"optionsBuilder\">A builder used to create or modify options for this context. Cannot be null.</param>");
            file.AppendLine("        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){");
            file.AppendLine("            base.OnConfiguring(optionsBuilder);");
            file.AppendLine("        }");
            if (hasDbSets)
            {
                foreach (Entity entity in conceptualModel.Entities.Values.Where(x => x.Used).OrderBy(x => x.Schema).ThenBy(x => x.Name))
                {

                    file.AppendLine("        /// <summary>");
                    file.Append("        /// Gets or sets the collection of <see cref=\"Domain.");
                    file.Append(parameters.DbName);
                    file.Append(".");
                    file.Append(HasSchema(entity.Schema) ? entity.Schema : DefaultSchema);
                    file.Append(".");
                    file.Append(entity.NameFixed);
                    file.AppendLine("\"/> entities in the database context.");
                    file.AppendLine("        /// </summary>");
                    file.Append("        public DbSet<Domain.");
                    file.Append(parameters.DbName);
                    file.Append(".");
                    file.Append(HasSchema(entity.Schema) ? entity.Schema : DefaultSchema);
                    file.Append(".");
                    file.Append(entity.NameFixed);
                    file.Append("> ");
                    file.Append(entity.NameFixed);
                    if (
                        entity.Name.EndsWith("s", StringComparison.InvariantCultureIgnoreCase)
                        //|| entity.NameFixed.EndsWith("1", StringComparison.InvariantCultureIgnoreCase)
                        )
                    {
                        //we dont need to do somthing else...
                    }
                    else if (entity.Name.EndsWith("a", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("e", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("i", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("o", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("u", StringComparison.InvariantCultureIgnoreCase)


                        || entity.Name.EndsWith("h", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("k", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("c", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("f", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("g", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("r", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("b", StringComparison.InvariantCultureIgnoreCase)
                        || entity.Name.EndsWith("t", StringComparison.InvariantCultureIgnoreCase)
                        )
                    {
                        file.Append("s");
                    }
                    else if (entity.Name.EndsWith("z", StringComparison.InvariantCultureIgnoreCase))
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
            file.AppendLine("        /// <summary>");
            file.AppendLine("        /// Configures the entity model for the context by applying entity type configurations.");
            file.AppendLine("        /// </summary>");
            file.AppendLine("        /// <remarks>This method is called by Entity Framework when the model for the context is being");
            file.AppendLine("        /// created. It applies all entity type configurations required for the application's data model. Override this");
            file.AppendLine("        /// method to customize the model and configure additional mappings or constraints as needed.</remarks>");
            file.AppendLine("        /// <param name=\"modelBuilder\" > The builder used to construct the model for the context. Cannot be null.</param>");
            file.AppendLine("        protected override void OnModelCreating(ModelBuilder modelBuilder){");
            file.AppendLine("            base.OnModelCreating(modelBuilder);");
            foreach (Entity entity in conceptualModel.Entities.Values.Where(x => x.Used).OrderBy(x => x.Schema).ThenBy(x => x.Name))
            {
                file.Append("            modelBuilder.ApplyConfiguration(new Configuration.");
                file.Append(parameters.DbName);
                file.Append(".");
                file.Append(HasSchema(entity.Schema) ? entity.Schema : DefaultSchema);
                file.Append(".");
                file.Append(entity.NameFixed);
                file.AppendLine("Configuration());");

            }
            if (conceptualModel.Functions.Count > 0)
            {
                file.AppendLine(Environment.NewLine);
                file.AppendLine("            // Configuration for function or store procedures: ");
                foreach (Function fn in conceptualModel.Functions.Values.OrderBy(x => x.IsFunction))
                {
                    if (fn.IsFunction)
                    {
                        file.Append("            modelBuilder.HasDbFunction(typeof(DbContext");
                        file.Append(parameters.DbName);
                        file.AppendLine(").GetMethod(nameof(");
                        file.Append(fn.NameFixed);
                        file.Append(")");
                        if (fn.Parameters.Count > 0)
                        {
                            file.AppendLine(",");
                            file.Append("                    new[] { ");
                            file.Append(string.Join(", ", from i in fn.Parameters select "typeof(" + i.Type + ")"));
                            file.AppendLine(" }))");
                        }
                        file.Append("                .HasName(\"");
                        file.Append(fn.Name);
                        file.AppendLine("\")");
                        file.Append("                .HasSchema(\"");
                        file.Append(storageModel.Functions[fn.Name].Schema);
                        file.AppendLine("\")");
                    }
                    else
                    {
                        StorageFunction sfn = storageModel.Functions[fn.Name];
                        string returnType = (fn.ReturnComplexType?.Replace("Collection(", string.Empty).Replace(")", string.Empty) ?? string.Empty).Replace("_", string.Empty);
                        file.Append("            modelBuilder.ApplyConfiguration(new Configuration.");
                        file.Append(parameters.DbName);
                        file.Append(".");
                        file.Append(HasSchema(sfn.Schema) ? sfn.Schema : DefaultSchema);
                        file.Append(".Procedure");
                        file.Append(returnType);
                        file.AppendLine("Configuration());");
                    }
                    file.AppendLine(Environment.NewLine);
                }
            }
            file.AppendLine("        }");

            if (conceptualModel.Functions.Count > 0)
            {
                file.AppendLine("   // Function or Store Procedures: ");
                foreach (Function fn in conceptualModel.Functions.Values)
                {
                    string returnDataType = fnCalculateReturnType(fn, parameters);
                    file.AppendLine("        /// <summary>");
                    file.AppendLine("        ///    ");
                    file.AppendLine("        /// </summary>");
                    file.Append("        public ");
                    file.Append(returnDataType);
                    file.Append(" ");
                    file.Append(fn.NameFixed);
                    file.Append("(");
                    file.Append(
                        string.Join(", ", fn.Parameters.Where(x => x.Direction != Direction.Out).Select(p => p.Type + " " + p.Name.ToLower()))
                    );
                    file.AppendLine(")");
                    file.AppendLine("        {");
                    if (fn.IsFunction)
                    {
                        file.AppendLine("            throw new NotSupportedException();");
                    }
                    else
                    {
                        StorageFunction sfn = storageModel.Functions[fn.Name];
                        string returnType = (fn.ReturnComplexType?.Replace("Collection(", string.Empty).Replace(")", string.Empty) ?? string.Empty).Replace("_", string.Empty);
                        foreach (FunctionParameter parameter in fn.Parameters)
                        {
                            file.Append("            SqlParameter param");
                            file.Append(parameter.Name);
                            file.AppendLine(" = new SqlParameter{");
                            file.Append("                ParameterName = \"@");
                            file.Append(parameter.Name);
                            file.Append("\",");
                            file.Append("                SqlDbType = SqlDbType.");
                            file.Append(parameter.Type[0].ToString().ToUpper());
                            file.Append(parameter.Type.Substring(1));
                            file.Append("                Value = ");
                            file.Append(parameter.Name.ToLower());
                            file.Append("                Direction = ParameterDirection.");
                            switch (parameter.Direction)
                            {
                                case Direction.In:
                                    file.Append("Input");
                                    break;
                                case Direction.Out:
                                    file.Append("Output");
                                    break;
                                case Direction.InOut:
                                    file.Append("InputOutput");
                                    break;
                            }
                            file.AppendLine("            };");
                        }
                        if (string.IsNullOrEmpty(returnType))
                        {
                            file.AppendLine("            Database.ExecuteSqlInterpolated($\"EXECUTE ");
                        } else
                        {
                            file.Append("            ");
                            file.Append(returnType);
                            file.Append(".FromSqlInterpolated($\"EXECUTE ");
                        }
                        file.Append(sfn.Schema);
                        file.Append(".");
                        file.Append(fn.Name);
                        file.Append(" ");
                        file.Append(string.Join(",", from f in fn.Parameters select "{param" + f.Name + "}"));
                        file.AppendLine("\");");

                        file.AppendLine("");
                        file.AppendLine("");
                        file.AppendLine("");
                    }
                    file.AppendLine("        }");
                }
            }
            file.AppendLine("    }");
            file.Append("}");
            File.WriteAllText(Path.Combine(parameters.DestinationPath, name + ".cs"), file.ToString());
        }
        /// <summary>
        /// Determines the return type for the specified function based on its metadata and UI parameters.
        /// </summary>
        /// <remarks>If the function does not specify a complex return type, the method infers the return
        /// type from output or input/output parameters, defaulting to "int" if none are found. For functions with a
        /// complex return type, the method constructs the appropriate collection or type name using the provided
        /// database and schema context.</remarks>
        /// <param name="fn">The function metadata object containing information about parameters, return type, and function
        /// characteristics.</param>
        /// <param name="parameters">The UI parameters that provide database and schema context for constructing the return type.</param>
        /// <returns>A string representing the calculated return type for the function. The value may be a primitive type, a
        /// complex type, or a collection type depending on the function's metadata.</returns>
        private string fnCalculateReturnType(Function fn, UIParameters parameters)
        {
            if (string.IsNullOrEmpty(fn.ReturnComplexType))
            {
                List<FunctionParameter> lstOut = fn.Parameters.Where(x => x.Direction == Direction.Out).ToList();
                List<FunctionParameter> lstInOut = fn.Parameters.Where(x => x.Direction == Direction.InOut).ToList();
                if (lstOut.Count == 1)
                {
                    return lstOut.First().Type ?? "int";
                }
                if (lstInOut.Count == 1)
                {
                    return lstInOut.First().Type ?? "int";
                }
                return "int";
            }
            string collectionType = fn.IsFunction ? "IQueryable" : "IEnumerable";
            if (fn.ReturnComplexType.StartsWith("Collection("))
            {
                return fn.ReturnComplexType.EndsWith("_Result)") ?
                    $"{collectionType}<Domain.{parameters.DbName}.{DefaultSchema}.{fn.NameFixed}Result>" :
                    $"{collectionType}<{fn.ReturnComplexType.Replace("Collection(", string.Empty).Replace(")", string.Empty)}>";
            }
            return fn.ReturnComplexType.Replace("Collection(", string.Empty).Replace(")", string.Empty);
        }
        /// <summary>
        /// Determines the multiplicity represented by the specified string value.
        /// </summary>
        /// <param name="value">A string that specifies the multiplicity. Supported values are "1", "0..1", "0..*", and "*".</param>
        /// <returns>A <see cref="Multiplicity"/> value corresponding to the specified string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> does not match a recognized multiplicity format.</exception>
        private Multiplicity GetMultiplicity(string value)
        {
            return value switch
            {
                "1" => Multiplicity.One,
                "0..1" => Multiplicity.ZeroOrOne,
                "0..*" => Multiplicity.ZeroOrMany,
                "*" => Multiplicity.Many,
                _ => throw new InvalidOperationException($"The multiplicity value '{value}' is not recognized."),
            };
        }
        /// <summary>
        /// Analyzes the provided table, entity, and mapping collections to establish relationships and update metadata
        /// for use in context mapping.
        /// </summary>
        /// <remarks>This method updates the 'Used' and naming metadata for tables and entities based on
        /// the provided mappings. It also ensures that foreign key and navigation property names are unique within
        /// their respective contexts. All referenced tables and entities must be present in the input dictionaries;
        /// otherwise, exceptions are thrown to indicate missing definitions.</remarks>
        /// <param name="tables">A dictionary containing table definitions, keyed by table name. Each table must be defined prior to
        /// analysis.</param>
        /// <param name="entities">A dictionary containing entity definitions, keyed by entity name. Each entity must be defined prior to
        /// analysis.</param>
        /// <param name="mappings">A list of mapping objects that define the relationships between tables and entities, including property and
        /// foreign key mappings.</param>
        /// <exception cref="InvalidOperationException">Thrown if a table referenced in a mapping is not defined in the tables dictionary, or if an entity
        /// referenced in a mapping or navigation property is not defined in the entities dictionary.</exception>
        /// <exception cref="Exception">Thrown if a critical error occurs during foreign key or navigation property name assignment, such as an
        /// incorrect index calculation resulting in a naming conflict.</exception>
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
                entity.NameFixed = NameInPascalCase(entity.Name, table.RepoType == RepoType.View);
                table.EntityName = entity.NameFixed;
                entity.TableName = map.TableName;
                entity.Schema = table.Schema;
                foreach (MappingProperty item in map.Properties)
                {
                    table.Columns[item.ColumnName].PropertyName = item.PropertyName;
                }
                if (table.ForeingKeys.Count > 0)
                {
                    HashSet<string> fkNames = new HashSet<string>();
                    foreach (Property property in entity.Properties)
                    {
                        fkNames.Add(property.Name);
                    }
                    fkNames.Add(table.NameFixed);
                    foreach (ForeingKey fk in table.ForeingKeys)
                    {
                        if (!fkNames.Contains(fk.TableNameFixedWithCounter))
                        {
                            fkNames.Add(fk.TableNameFixedWithCounter);
                            continue;
                        }
                        int index = 1;
                        while (fkNames.Contains(fk.TableNameFixed + index))
                        {
                            index++;
                        }
                        fk.TableNameFixedWithCounter = fk.TableNameFixed + index;
                        if (!fkNames.Contains(fk.TableNameFixedWithCounter))
                        {
                            fkNames.Add(fk.TableNameFixedWithCounter);
                        }
                        else
                        {
                            throw new Exception("critical error, index calculation is incorrect");
                        }
                    }
                }
            }

            foreach (Entity entity in entities.Values)
            {
                if (entity.NavigationProperties.Count > 0)
                {
                    int countRefNavProps = 0;
                    HashSet<string> fkNames = new HashSet<string>();
                    foreach (Property property in entity.Properties)
                    {
                        fkNames.Add(property.Name);
                    }
                    fkNames.Add(entity.NameFixed);
                    foreach (NavigationProperty navProp in entity.NavigationProperties)
                    {
                        if (entities.ContainsKey(navProp.EntityName))
                        {
                            Entity targetEntity = entities[navProp.EntityName];
                            Table targetTable = tables[targetEntity.TableName ?? string.Empty];
                            targetEntity.Used = true;
                            targetTable.Used = true;
                            navProp.TargetName = targetTable.Name;
                            navProp.TargetNameFixed = targetTable.NameFixed;
                            navProp.TargetNameFixedWithCounter = targetTable.NameFixed;
                            navProp.TargetSchema = targetTable.Schema;
                        }
                        else
                        {
                            throw new InvalidOperationException($"The entity '{navProp.EntityName}' is not defined in the C-S mapping context.");
                        }
                        if (!fkNames.Contains(navProp.TargetNameFixedWithCounter))
                        {
                            fkNames.Add(navProp.TargetNameFixedWithCounter);
                            continue;
                        }
                        countRefNavProps = 1;
                        while (fkNames.Contains(navProp.TargetNameFixed + countRefNavProps))
                        {
                            countRefNavProps++;
                        }
                        navProp.TargetNameFixedWithCounter = navProp.TargetNameFixed + countRefNavProps;
                        if (!fkNames.Contains(navProp.TargetNameFixedWithCounter))
                        {
                            fkNames.Add(navProp.TargetNameFixedWithCounter);
                        }
                        else
                        {
                            throw new Exception("critical error, index calculation is incorrect");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// SSDL content
        /// </summary>
        /// <returns>Tables list</returns>
        private StorageModel GetStorageModels(XElement? edmxRuntime)
        {
            StorageModel storageModel = new StorageModel
            {
                Tables = new Dictionary<string, Table>(),
                Functions = new Dictionary<string, StorageFunction>()
            };
            XElement? storageModels = edmxRuntime?.Elements().FirstOrDefault(n => n.Name.LocalName == "StorageModels");
            XElement? schema = storageModels?.Elements().FirstOrDefault(n => n.Name.LocalName == "Schema");
            string alias = (schema.Attribute("Alias")?.Value ?? string.Empty) + ".";
            if (schema == null)
            {
                throw new InvalidOperationException("The EDMX file does not contain any schemas for SSDL context.");
            }
            foreach (XElement entity in schema.Elements())
            {
                if (entity.Name.LocalName == "EntityType")
                {
                    Table table = new Table
                    {
                        Name = entity.Attribute("Name")?.Value ?? string.Empty
                    };
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
                    storageModel.Tables[table.Name] = table;
                }
                else if (entity.Name.LocalName == "Function")
                {
                    StorageFunction function = new StorageFunction
                    {
                        Name = entity.Attribute("Name")?.Value ?? string.Empty,
                        Schema = entity.Attribute("Schema")?.Value ?? "dbo",
                        IsFunction = entity.Attribute("IsComposable")?.Value == "true",
                        ReturningCollection = false
                    };
                    string temp = function.Name;
                    string preFix = string.Empty;
                    if (temp.StartsWith("SP", StringComparison.InvariantCultureIgnoreCase) || temp.StartsWith("PA", StringComparison.InvariantCultureIgnoreCase) || temp.StartsWith("FN", StringComparison.InvariantCultureIgnoreCase))
                    {
                        preFix = temp.Substring(0, 2).ToUpper();
                        //preFix = preFix[0].ToString().ToUpper() + preFix[1].ToString().ToLower();
                        temp = temp.Substring(2);
                    }
                    function.NameFixed = preFix + NameInPascalCase(temp, false);
                    foreach (XElement? property in entity.Descendants().Where(n => n.Name.LocalName == "Parameter"))
                    {
                        StorageParameter parameter = new StorageParameter
                        {
                            Name = property.Attribute("Name")?.Value ?? string.Empty,
                            Type = property.Attribute("Type")?.Value ?? string.Empty,
                        };
                        function.Parameters.Add(parameter);
                        switch (property.Attribute("Mode")?.Value)
                        {
                            case "In":
                                parameter.Direction = Direction.In;
                                break;
                            case "Out":
                                parameter.Direction = Direction.Out;
                                break;
                            case "InOut":
                                parameter.Direction = Direction.InOut;
                                break;
                            default:
                                parameter.Direction = Direction.In;
                                break;
                        }
                    }
                    storageModel.Functions.Add(function.Name, function);
                    XElement? returnType = entity?.Descendants().FirstOrDefault(n => n.Name.LocalName == "ReturnType");
                    if (returnType != null)
                    {
                        XElement? type = returnType.Elements().FirstOrDefault();
                        if (type != null)
                        {
                            function.ReturningCollection = type.Name.LocalName == "CollectionType";
                            XElement? rowType = type.Elements().FirstOrDefault();
                            if (rowType != null)
                            {
                                foreach (XElement? property in rowType.Descendants().Where(n => n.Name.LocalName == "Property"))
                                {
                                    Column column = new Column
                                    {
                                        Name = property.Attribute("Name")?.Value ?? string.Empty,
                                        Type = property.Attribute("Type")?.Value ?? string.Empty,
                                        //IsNullable = !(property.Attribute("Nullable")?.Value == "false"),
                                        //MaxLength = Convert.ToInt32(property.Attribute("MaxLength")?.Value ?? "0"),
                                        //Precision = Convert.ToInt32(property.Attribute("Precision")?.Value ?? "0"),
                                        //Scale = Convert.ToInt32(property.Attribute("Scale")?.Value ?? "0"),
                                    };
                                    //column.MaxLength = column.MaxLength == 0 ? null : column.MaxLength == -1 ? (int?)null : column.MaxLength;
                                    //column.MaxLength = column.Type == "varchar(max)" ? null : column.MaxLength;
                                    //column.MaxLength = column.Type == "varbinary(max)" ? null : column.MaxLength;
                                    function.ReturnColumns.Add(column);
                                }
                            }
                        }
                    }
                }
            }
            XElement? schemaNames = storageModels?.Descendants().FirstOrDefault(n => n.Name.LocalName == "EntityContainer");
            foreach (XElement entity in schemaNames.Elements())
            {
                if (entity.Name.LocalName == "EntitySet")
                {
                    string entityName = entity.Attribute("Name")?.Value ?? string.Empty;
                    if (storageModel.Tables.ContainsKey(entityName))
                    {
                        Table table = storageModel.Tables[entityName];
                        table.Schema = entity.Attribute("Schema")?.Value ?? "dbo";
                        table.RepoType = entity.Attribute(StoreNameSpace + "Type")?.Value.EndsWith("Views", StringComparison.InvariantCultureIgnoreCase) == true ? RepoType.View : RepoType.Table;
                        table.NameFixed = NameInPascalCase(table.Name, table.RepoType == RepoType.View);
                    }
                }
            }
            foreach (XElement entity in schema.Elements())
            {
                if (entity.Name.LocalName == "Association")
                {
                    string fkName = entity.Attribute("Name")?.Value ?? string.Empty;
                    List<XElement> elements = entity.Elements().ToList();
                    if (elements.Count != 3)
                    {
                        throw new InvalidOperationException("The EDMX file contains an invalid association definition.");
                    }
                    string? tableS = elements[0].Attribute("Type")?.Value.Replace(alias, string.Empty);
                    string? tableD = elements[1].Attribute("Type")?.Value.Replace(alias, string.Empty);
                    Multiplicity multiplicityS = GetMultiplicity(elements[0].Attribute("Multiplicity")?.Value);
                    Multiplicity multiplicityD = GetMultiplicity(elements[1].Attribute("Multiplicity")?.Value);

                    if (string.IsNullOrEmpty(tableS) || string.IsNullOrEmpty(tableD))
                    {
                        throw new InvalidOperationException("The EDMX file contains an invalid association definition. [1]");
                    }
                    Table tableSou = storageModel.Tables[tableS];
                    Table tableDes = storageModel.Tables[tableD];
                    if (tableSou == null || tableDes == null)
                    {
                        throw new InvalidOperationException("The EDMX file contains an invalid association definition. [2]");
                    }
                    XElement principal = elements[2].Elements().First(entity => entity.Name.LocalName == "Principal");
                    XElement dependent = elements[2].Elements().First(entity => entity.Name.LocalName == "Dependent");
                    ForeingKey foreingKeyS = new ForeingKey()
                    {
                        Source = multiplicityS,
                        Destination = multiplicityD,
                        Table = tableD,
                        TableSchema = tableDes.Schema,
                        TableNameFixed = NameInPascalCase(tableD, false),
                    };
                    foreingKeyS.TableNameFixedWithCounter = foreingKeyS.TableNameFixed;
                    ForeingKey foreingKeyD = new ForeingKey()
                    {
                        Source = multiplicityD,
                        Destination = multiplicityS,
                        Table = tableS,
                        TableSchema = tableSou.Schema,
                        TableNameFixed = NameInPascalCase(tableS, false),
                    };
                    foreingKeyD.TableNameFixedWithCounter = foreingKeyD.TableNameFixed;
                    tableSou.ForeingKeys.Add(foreingKeyS);
                    tableDes.ForeingKeys.Add(foreingKeyD);
                    List<XElement> propertiesS = principal.Elements().ToList();
                    List<XElement> propertiesD = dependent.Elements().ToList();
                    if (propertiesS == null || propertiesD == null || propertiesD.Count == 0 || propertiesD.Count != propertiesS.Count)
                    {
                        throw new InvalidOperationException("The EDMX file contains an invalid association definition. [3]");
                    }
                    for (int i = 0; i < propertiesS.Count; i++)
                    {
                        XElement sourceProp = propertiesS[i];
                        XElement destProp = propertiesD[i];
                        string sP = sourceProp.Attribute("Name")?.Value ?? string.Empty;
                        string dP = destProp.Attribute("Name")?.Value ?? string.Empty;
                        foreingKeyS.Columns.Add(new KeyValuePair<string, string>(sP, dP));
                        foreingKeyD.Columns.Add(new KeyValuePair<string, string>(dP, sP));
                    }
                }
            }
            return storageModel;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isView"></param>
        /// <returns></returns>
        private string NameInPascalCase(string name, bool isView)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }/*
            if (name.Length < 5)
            {
                return name.ToUpper();
            }*/
            if (isView)
            {
                if (name.StartsWith("view_", StringComparison.InvariantCultureIgnoreCase))
                {
                    name = name.Substring(5);
                }
                else if (name.StartsWith("vista_", StringComparison.InvariantCultureIgnoreCase))
                {
                    name = name.Substring(6);
                }
                else if (name.StartsWith("vista", StringComparison.InvariantCultureIgnoreCase))
                {
                    name = name.Substring(5);
                }
                else if (name.StartsWith("vw_", StringComparison.InvariantCultureIgnoreCase))
                {
                    name = name.Substring(3);
                }
                else if (name.StartsWith("vw", StringComparison.InvariantCultureIgnoreCase))
                {
                    name = name.Substring(2);
                }
                else if (name.StartsWith("v", StringComparison.InvariantCultureIgnoreCase))
                {
                    name = name.Substring(1);
                }
            }
            if (IsInPascalCase(name))
            {
                if (isView)
                {
                    name = "View" + name;
                }
                else if (name == "FLUJOvsDOCANEXO")
                {
                    return "FlujoVsDocAnexo";
                }
                else if (name == "FLUJOvsDOCUMENTO")
                {
                    return "FlujoVsDocumento";
                }
                return name.Replace("_", string.Empty);
            }
            // Replace underscores and spaces with a single space
            string[] parts = name
                .Replace("_", " ")
                .Replace("-", " ")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Convert each part to PascalCase
            StringBuilder pascalName = new StringBuilder();
            foreach (string part in parts)
            {
                if (part.Length == 0) continue;
                // Lowercase all except first letter
                pascalName.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                    pascalName.Append(part.Substring(1).ToLower());
            }
            string nameInPascalCase = pascalName.ToString();
            nameInPascalCase = nameInPascalCase.Replace("tms", "TMS", StringComparison.InvariantCultureIgnoreCase);
            nameInPascalCase = nameInPascalCase.Replace("xml", "XML", StringComparison.InvariantCultureIgnoreCase);
            nameInPascalCase = nameInPascalCase.Replace("xsl", "XSL", StringComparison.InvariantCultureIgnoreCase);
            nameInPascalCase = nameInPascalCase.Replace("tsol", "TSOL", StringComparison.InvariantCultureIgnoreCase);
            nameInPascalCase = nameInPascalCase.Replace("VOLRUTA", "VolRuta", StringComparison.InvariantCultureIgnoreCase);
            nameInPascalCase = nameInPascalCase.Replace("wsgen", "WSGen", StringComparison.InvariantCultureIgnoreCase);
            nameInPascalCase = nameInPascalCase.EndsWith("doc") ? nameInPascalCase.Replace("doc", "Doc", StringComparison.InvariantCultureIgnoreCase) : nameInPascalCase;
            if (isView)
            {
                nameInPascalCase = "View" + nameInPascalCase;
            }
            return nameInPascalCase;
        }
        /// <summary>
        /// Determines whether the specified string is in Pascal case format.
        /// </summary>
        /// <remarks>Pascal case requires the first character to be uppercase, followed by lowercase
        /// characters. This method performs a heuristic check and may not validate all edge cases of Pascal case
        /// formatting.</remarks>
        /// <param name="name">The string to evaluate for Pascal case formatting. Cannot be null or whitespace.</param>
        /// <returns>true if the input string is in Pascal case; otherwise, false.</returns>
        private bool IsInPascalCase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            return
                (char.IsUpper(name[0]) && (char.IsLower(name[1]) || (name.Length > 5 && char.IsLower(name[4]) && char.IsLower(name[5])))) ||
                (char.IsUpper(name[1]) && (char.IsLower(name[2]) || (name.Length > 6 && char.IsLower(name[5]) && char.IsLower(name[6]))));
        }
        /// <summary>
        /// Creates a new NavigationProperty instance representing a relationship to the specified entity with the given
        /// multiplicity.
        /// </summary>
        /// <param name="entity">The entity to which the navigation property will refer. Cannot be null.</param>
        /// <param name="multiplicity">The multiplicity of the relationship, indicating how many related entities are allowed.</param>
        /// <returns>A NavigationProperty object configured with the specified entity name and multiplicity.</returns>
        private NavigationProperty CreateNavigationProperty(Entity entity, Multiplicity multiplicity)
        {
            return new NavigationProperty()
            {
                Multiplicity = multiplicity,
                EntityName = entity.Name
            };
        }
        /// <summary>
        /// SSDL content
        /// </summary>
        /// <returns>Tables list</returns>
        private ConceptualModel GetConceptualModels(XElement? edmxRuntime)
        {
            ConceptualModel conceptualModel = new ConceptualModel
            {
                Entities = new Dictionary<string, Entity>(),
                Functions = new Dictionary<string, Function>(),
                ComplexTypes = new Dictionary<string, ComplexType>(),
            };
            XElement? conceptualModels = edmxRuntime?.Descendants().FirstOrDefault(n => n.Name.LocalName == "ConceptualModels");
            XElement? schema = conceptualModels?.Descendants().FirstOrDefault(n => n.Name.LocalName == "Schema");
            XElement? namespaceNode = conceptualModels?.Descendants().FirstOrDefault(n => n.Name.LocalName == "Namespace");
            if (schema == null)
            {
                throw new InvalidOperationException("The EDMX file does not contain any schemas for SSDL context.");
            }
            string namespaceName = (schema.Attribute("Namespace")?.Value ?? string.Empty) + ".";
            foreach (XElement? entityType in schema.Elements())
            {
                if (entityType.Name.LocalName == "EntityType")
                {
                    Entity entity = new Entity
                    {
                        Name = entityType.Attribute("Name")?.Value ?? string.Empty
                    };
                    entity.TableName = entity.Name;
                    if (entity.Name == "Regla1")
                    {
                        entity.Name = "REGLA";
                    }
                    else if (entity.Name == "REGLA")
                    {
                        entity.Name = "Regla1";
                    }
                    //entity.NameFixed = NameInPascalCase(entity.Name, false);
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
                    conceptualModel.Entities[entity.Name] = entity;
                }
                else if (entityType.Name.LocalName == "ComplexType")
                {
                    ComplexType complexType = new ComplexType
                    {
                        Name = entityType.Attribute("Name")?.Value ?? string.Empty
                    };
                    string temp = complexType.Name;
                    string preFix = string.Empty;
                    if (temp.StartsWith("SP", StringComparison.InvariantCultureIgnoreCase) || temp.StartsWith("PA", StringComparison.InvariantCultureIgnoreCase))
                    {
                        preFix = temp.Substring(0, 2);
                        temp = temp.Substring(2);
                    }
                    complexType.NameFixed = preFix + NameInPascalCase(temp, false);
                    foreach (XElement parameter in entityType.Elements().Where(n => n.Name.LocalName == "Property"))
                    {
                        Property param = new Property
                        {
                            Name = parameter.Attribute("Name")?.Value ?? string.Empty,
                            Type = parameter.Attribute("Type")?.Value ?? string.Empty,
                            IsNullable = !(parameter.Attribute("Nullable")?.Value == "false"),
                            MaxLength = Convert.ToInt32(parameter.Attribute("MaxLength")?.Value ?? "0"),
                            Precision = Convert.ToInt32(parameter.Attribute("Precision")?.Value ?? "0"),
                            Scale = Convert.ToInt32(parameter.Attribute("Scale")?.Value ?? "0"),
                        };
                        complexType.Properties.Add(param);
                    }
                    conceptualModel.ComplexTypes.Add(complexType.Name, complexType);
                }
                else if (entityType.Name.LocalName == "EntityContainer")
                {
                    foreach (XElement? functionImport in entityType.Elements().Where(n => n.Name.LocalName == "FunctionImport"))
                    {
                        Function function = new Function
                        {
                            Name = functionImport.Attribute("Name")?.Value ?? string.Empty,
                            ReturnComplexType = functionImport.Attribute("ReturnType")?.Value,
                            IsFunction = functionImport.Attribute("IsComposable")?.Value == "true"
                        };
                        string temp = function.Name;
                        string preFix = string.Empty;
                        if (temp.StartsWith("SP", StringComparison.InvariantCultureIgnoreCase) || temp.StartsWith("PA", StringComparison.InvariantCultureIgnoreCase) || temp.StartsWith("FN", StringComparison.InvariantCultureIgnoreCase))
                        {
                            preFix = temp.Substring(0, 2).ToUpper();
                            //preFix = preFix[0].ToString().ToUpper() + preFix[1].ToString().ToLower();
                            temp = temp.Substring(2);
                        }
                        function.NameFixed = preFix + NameInPascalCase(temp, false);
                        conceptualModel.Functions.Add(function.Name, function);
                        foreach (XElement? parameter in functionImport.Elements().Where(n => n.Name.LocalName == "Parameter"))
                        {
                            FunctionParameter param = new FunctionParameter
                            {
                                Name = parameter.Attribute("Name")?.Value ?? string.Empty,
                                Type = parameter.Attribute("Type")?.Value ?? string.Empty,
                            };
                            switch (parameter.Attribute("Mode")?.Value)
                            {
                                case "In":
                                    param.Direction = Direction.In;
                                    break;
                                case "Out":
                                    param.Direction = Direction.Out;
                                    break;
                                case "InOut":
                                    param.Direction = Direction.InOut;
                                    break;
                                default:
                                    param.Direction = Direction.In;
                                    break;
                            }
                            function.Parameters.Add(param);
                        }
                    }
                }
            }
            foreach (XElement? association in schema.Descendants().Where(n => n.Name.LocalName == "Association"))
            {
                List<XElement> elements = association.Elements().ToList();
                if (elements.Count < 3)
                {
                    continue;
                    //throw new InvalidOperationException("The EDMX file contains an invalid association definition.");
                }
                string? entityS = elements[0].Attribute("Type")?.Value.Replace(namespaceName, string.Empty);
                string? entityD = elements[1].Attribute("Type")?.Value.Replace(namespaceName, string.Empty);
                Multiplicity multiplicityS = GetMultiplicity(elements[0].Attribute("Multiplicity")?.Value);
                Multiplicity multiplicityD = GetMultiplicity(elements[1].Attribute("Multiplicity")?.Value);
                if (string.IsNullOrEmpty(entityS) || string.IsNullOrEmpty(entityD))
                {
                    throw new InvalidOperationException("The EDMX file contains an invalid association definition. [1]");
                }
                Entity entitySou = conceptualModel.Entities[entityS];
                Entity entityDes = conceptualModel.Entities[entityD];
                if (entitySou == null || entityDes == null)
                {
                    throw new InvalidOperationException("The EDMX file contains an invalid association definition. [2]");
                }
                entitySou.NavigationProperties.Add(CreateNavigationProperty(entityDes, multiplicityD));
                entityDes.NavigationProperties.Add(CreateNavigationProperty(entitySou, multiplicityS));
                /*
                XElement principal = elements[2].Elements().First(entity => entity.Name.LocalName == "Principal");
                XElement dependent = elements[2].Elements().First(entity => entity.Name.LocalName == "Dependent");
                List<XElement> propertiesS = principal.Elements().ToList();
                List<XElement> propertiesD = dependent.Elements().ToList();
                if (propertiesS == null || propertiesD == null || propertiesD.Count == 0 || propertiesD.Count != propertiesS.Count)
                {
                    throw new InvalidOperationException("The EDMX file contains an invalid association definition. [3]");
                }
                for (int i = 0; i < propertiesS.Count; i++)
                {
                    foreingKeyS.FkProperties.Add(propertiesS[i].Attribute("Property")?.Value ?? string.Empty);
                    foreingKeyD.FkProperties.Add(propertiesD[i].Attribute("Property")?.Value ?? string.Empty);
                }
                */
            }
            return conceptualModel;
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
                    EntityName = (storeEntitySet.Attribute("TypeName")?.Value ?? string.Empty).Split(".")[1]
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
    /// <summary>
    /// Represents a set of parameters used to configure user interface-driven code generation operations, including
    /// file paths, database settings, and generation options.
    /// </summary>
    /// <remarks>This class encapsulates options for specifying source and destination file locations, target
    /// namespace, database name, and flags that control which components are generated (such as DbContext, models,
    /// repositories, and configurations). Some properties are intended to be set internally by the application logic
    /// and may not be directly modifiable by consumers. Use this type to pass configuration data between UI layers and
    /// code generation routines.</remarks>
    public class UIParameters
    {
        public string FilePath { get; set; }
        public string DestinationPath { get; set; }
        public string Namespace { get; set; }
        public string DbName { get; set; }
        public bool CreateDbContext { get; internal set; }
        public bool CreateModels { get; internal set; }
        public bool CreateRepositories { get; internal set; }
        public bool CreateConfigurations { get; internal set; }
    }
    public class StorageModel
    {
        public IDictionary<string, Table> Tables { get; set; }
        public IDictionary<string, StorageFunction> Functions { get; set; }
    }

    public class ConceptualModel
    {
        public IDictionary<string, Entity> Entities { get; set; }
        public IDictionary<string, Function> Functions { get; set; }
        public IDictionary<string, ComplexType> ComplexTypes { get; set; }
    }
}
