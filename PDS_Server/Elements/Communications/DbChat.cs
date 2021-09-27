using System;
using System.Collections.Generic;

namespace PDS_Server.Elements.Communications
{
    public class DbChat : DbElement
    {
        public DbChat() { }
        public List<DbMessage> Messages { get; set; }
        public DateTime LastChange { get; set; } = DateTime.UtcNow;
    }
}
