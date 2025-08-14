namespace EDMXMigrationTool
{
    internal class Mapping
    {
        public string TableName { get; set; }
        public string EntityName { get; set; }
        public IList<MappingProperty> Properties { get; set; } = new List<MappingProperty>();
    }
    internal class MappingProperty
    {
        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
    }
}
