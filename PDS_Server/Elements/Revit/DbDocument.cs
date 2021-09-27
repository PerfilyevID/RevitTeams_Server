using MongoDB.Bson;

namespace PDS_Server.Elements.Revit
{
    public class DbDocument : DbElement
    {
        public DbDocument() { }
        public ObjectId? Project { get; set; }
        public ObjectId? Department { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string ServerGuid { get; set; }
        public bool IsCloudModel { get; set; }
        public int SyncCount { get; set; } = 0;
        public ObjectId? FoundByUser { get; set; }
    }
}
