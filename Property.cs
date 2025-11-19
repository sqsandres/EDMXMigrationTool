namespace EDMXMigrationTool
{
    public class Property
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsForeingKey { get; set; }
        public string? DefaultValue { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public bool FixedLength { get; internal set; }
        public bool Unicode { get; internal set; }
        public override string ToString()
        {
            return $"{Name} Type:{Type} IsNullable:{IsNullable} IsPrimaryKey:{IsPrimaryKey} DefaultValue:{DefaultValue} MaxLength:{MaxLength} Precision:{Precision} Scale:{Scale} FixedLength:{FixedLength} Unicode:{Unicode}";
        }
    }
}
