namespace EDMXMigrationTool
{
    internal class MappingContext
    {
        public IList<Mapping> Tables { get; set; } = new List<Mapping>();
        public IList<MappingFunction> Function { get; set; } = new List<MappingFunction>();

    }
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
    internal class MappingFunction
    {
        public string FunctionName { get; set; }
        public string StoredProcedureName { get; set; }
        public override string ToString()
        {
            return $"Function:{FunctionName} == StoredProcedure:{StoredProcedureName}";
        }
    }
}
