using MongoDB.Bson;
using System;

namespace PDS_Server.Elements.Revit
{
    public class DbClashResult : DbElement
    {
        public DbClashResult() { }
        public ObjectId? Group { get; set; }
        public DbClash[] Clashes { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreationTime { get; set; } = DateTime.UtcNow;
        public DateTime LastChange { get; set; } = DateTime.UtcNow;
        public int ItemsCount { get; set; }
        public int ItemsDone { get; set; }
    }
}
