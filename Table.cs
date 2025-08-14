using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDMXMigrationTool
{
    internal class Table
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public List<Column> Columns { get; set; } = new List<Column>();
        public bool Used { get; set; } = false;
    }
}
