namespace EDMXMigrationTool
{
    public class ForeingKey
    {
        public Multiplicity Source { get; set; }
        public Multiplicity Destination { get; set; }
        public required string Table { get; set; }
        public string? TableNameFixed { get; set; }
        public string? TableNameFixedWithCounter { get; set; }
        public List<KeyValuePair<string, string>> Columns { get; set; } = [];
        public string? TableSchema { get; set; }

        public override string ToString()
        {
            return $"Table:{Table} Columns:[{string.Join(", ", Columns.Select(c => c.Key + " == " + c.Value))}]";
        }
    }
    public enum Multiplicity
    {
        One,
        ZeroOrOne,
        ZeroOrMany,
        Many
    }
}
