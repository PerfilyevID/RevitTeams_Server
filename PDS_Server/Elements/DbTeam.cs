using MongoDB.Bson;

namespace PDS_Server.Elements
{
    public class DbTeam : DbElement
    {
        public DbTeam() { }
        public string Name { get; set; }
        public ObjectId? Owner { get; set; }
        public ObjectId[] Users { get; set; }
        public int MaxCount { get; set; }
    }
}
