namespace EDMXMigrationTool
{
    internal class Table
    {
        public string Name { get; set; }
        public string NameFixed { get; set; }
        public string Schema { get; set; }
        public string EntityName { get; set; }
        public Dictionary<string, Column> Columns { get; set; } = new Dictionary<string, Column>();
        public bool Used { get; set; } = false;
    }
}
