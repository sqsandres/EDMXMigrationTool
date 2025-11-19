namespace EDMXMigrationTool
{
    internal class Mapping
    {
        public string TableName { get; set; }
        public string EntityName { get; set; }
        public IList<MappingProperty> Properties { get; set; } = new List<MappingProperty>();
        public override string ToString()
        {
            return $"Table:{TableName} == Entity:{EntityName}";
        }
    }
    internal class MappingProperty
    {
        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
        public override string ToString()
        {
            return $"Column:{ColumnName} == Property:{PropertyName}";
        }
    }
}
