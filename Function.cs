namespace EDMXMigrationTool
{
    public class Function
    {
        public required string Name { get; set; }
        public List<FunctionParameter> Parameters { get; set; } = new List<FunctionParameter>();
        public string? NameFixed { get; set; }
        public string? ReturnComplexType { get; set; }
        public bool IsFunction { get; set; }
        public override string ToString()
        {
            return $"{Name} - {NameFixed}";
        }
    }
    public class FunctionParameter
    {
        public required string Name { get; set; }
        public string? Type { get; set; }
        public Direction Direction { get; set; }
        public bool IsNullable { get; set; }
        public override string ToString()
        {
            return $"{Name} - {Type}";
        }
    }
    public class ComplexType
    {
        public required string Name { get; set; }
        public string? NameFixed { get; set; }
        public List<Property> Properties { get; set; } = new List<Property>();
        public override string ToString()
        {
            return $"{Name} - {NameFixed}";
        }
    }
}
