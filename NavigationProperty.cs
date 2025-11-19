namespace EDMXMigrationTool
{
    public class NavigationProperty
    {
        public string? EntityName { get; set; }
        public string? TargetName { get; set; }
        public string? TargetNameFixed { get; internal set; }
        public string? TargetNameFixedWithCounter { get; internal set; }
        public string? TargetSchema { get; internal set; }
        public Multiplicity Multiplicity { get; set; }
        public override string ToString()
        {
            return $"TargetName:{TargetName} TargetNameFixed:{TargetNameFixed} TargetSchema:{TargetSchema} Multiplicity:{Multiplicity} TableNameFixedWithCounter:{TargetNameFixedWithCounter}";
        }
    }
}
