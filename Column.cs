namespace EDMXMigrationTool
{
    public class Column
    {
        public string? Name { get; set; }
        public string? PropertyName { get; set; }
        public string? Type { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string? DefaultValue { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public string? ValueGenerator { get; internal set; }
        public override string ToString()
        {
            return $"Name:{Name} PropertyName:{PropertyName} Type:{Type} IsNullable:{IsNullable} IsPrimaryKey:{IsPrimaryKey} DefaultValue:{DefaultValue} MaxLength:{MaxLength} Precision:{Precision} Scale:{Scale} ValueGenerator:{ValueGenerator}";
        }
    }
}
