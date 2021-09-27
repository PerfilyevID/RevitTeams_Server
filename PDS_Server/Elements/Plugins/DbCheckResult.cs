using MongoDB.Bson;

namespace PDS_Server.Elements.Plugins
{
    public class DbCheckResult : DbElement
    {
        public DbCheckResult() { }
        public ObjectId? Project { get; set; }
        public ObjectId? Document { get; set; }
        public DbCheckRow[] Data { get; set; }
    }
}
