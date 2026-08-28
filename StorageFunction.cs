namespace EDMXMigrationTool
{
    public class StorageFunction
    {
        public required string Name { get; set; }
        public string? NameFixed { get; set; }
        public string? Schema { get; set; }
        public List<StorageParameter> Parameters { get; set; } = new List<StorageParameter>();
        public bool IsFunction { get; internal set; }
        public bool ReturningCollection { get; internal set; }
        public List<Column> ReturnColumns { get; set; } = new List<Column>();
        public override string ToString()
        {
            return $"{Name} - {NameFixed} - {Schema}";
        }
    }
    public class StorageParameter
    {
        public required string Name { get; set; }
        public string? Type { get; set; }
        public Direction Direction { get; set; }
        public override string ToString()
        {
            return $"{Name} - {Type} - {Direction}";
        }
    }
    public enum Direction
    {
        In,
        Out,
        InOut
    }
}

