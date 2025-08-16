namespace EDMXMigrationTool
{
    internal class Table
    {
        public string Name { get; set; }
        public string NameFixed { get; set; }
        public string Schema { get; set; }
        public List<Column> Columns { get; set; } = new List<Column>();
        public bool Used { get; set; } = false;
    }
}
