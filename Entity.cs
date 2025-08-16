namespace EDMXMigrationTool
{
    internal class Entity
    {
        public required string Name { get; set; }
        public required string TableName { get; set; }
        public required string Schema { get; set; }
        public List<Property> Properties { get; set; } = new List<Property>();
        public string NameFixed { get; set; }
        public bool Used { get; set; } = false;
        //public List<NavigationProperty> NavigationProperties { get; set; } = new List<NavigationProperty>();
    }
}
