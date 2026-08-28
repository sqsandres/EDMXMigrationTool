namespace EDMXMigrationTool
{
    public class Table
    {
        public required string Name { get; set; }
        public string? NameFixed { get; set; }
        public string? Schema { get; set; }
        public string? EntityName { get; set; }
        public Dictionary<string, Column> Columns { get; set; } = new Dictionary<string, Column>();
        public bool Used { get; set; } = false;
        public RepoType RepoType { get; set; } = RepoType.Table;
        public IList<ForeingKey> ForeingKeys { get; set; } = new List<ForeingKey>();
        public override string ToString()
        {
            return $"{Name} EntityName:{EntityName} Schema:{Schema} NameFixed:{NameFixed}";
        }
    }
    public enum RepoType
    {
        Table,
        View
    }
}
