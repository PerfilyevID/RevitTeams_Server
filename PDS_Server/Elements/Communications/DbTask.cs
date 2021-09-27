using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace PDS_Server.Elements.Communications
{
    public class DbTask : DbElement
    {
        public DbTask() { }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageData { get; set; }
        public string GltfData { get; set; }
        public Dictionary<string, bool>[] CheckLists { get; set; }
        public ObjectId Chat { get; set; }
        public ObjectId From { get; set; }
        public ObjectId[] Watchers { get; set; }
        public ObjectId[] To { get; set; }
        public DateTime? ExpirationTime { get; set; }
    }
}
