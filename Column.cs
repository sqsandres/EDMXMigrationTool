namespace EDMXMigrationTool
{
    internal class Column
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string? DefaultValue { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public string? ValueGenerator { get; internal set; }
        //StoreGeneratedPattern="Computed" 
        //StoreGeneratedPattern="Identity"
    }
}
