using MongoDB.Bson;
using System;

namespace PDS_Server.Elements.Communications
{
    public class DbMessage
    {
        public DbMessage() { }
        public ObjectId? From { get; set; }
        public ObjectId? To { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;
    }
}
