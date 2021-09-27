using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PDS_Server.Elements
{
    public class DbNews : DbElement
    {
        public DbNews() { }
        public string Content { get; set; }
        public DateTime Time { get; set; }
        public bool Published { get; set; }
    }
}
