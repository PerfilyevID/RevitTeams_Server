using MongoDB.Bson;

namespace PDS_Server.Elements.Revit
{
    public class DbGroupOfClashes : DbElement
    {
        public DbGroupOfClashes() { }
        public ObjectId? Project { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Progress { get; set; }
        public ObjectId[] Items { get; set; }
        public bool IsClosed { get; set; }
    }
}
