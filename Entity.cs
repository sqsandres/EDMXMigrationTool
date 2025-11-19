namespace EDMXMigrationTool
{
    public class Entity
    {
        public required string Name { get; set; }
        public string? TableName { get; set; }
        public string? Schema { get; set; }
        public IList<Property> Properties { get; set; } = new List<Property>();
        public string? NameFixed { get; set; }
        public bool Used { get; set; } = false;
        public IList<NavigationProperty> NavigationProperties { get; set; } = new List<NavigationProperty>();
        public override string ToString()
        {
            return $"{Name} - {TableName} Schema:{Schema} NameFixed:{NameFixed}";
        }
    }
}
