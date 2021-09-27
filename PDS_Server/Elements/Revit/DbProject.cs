using MongoDB.Bson;

namespace PDS_Server.Elements.Revit
{
    public class DbProject : DbElement
    {
        public DbProject() { }
        public string Name { get; set; }
        public string Color { get; set; }
        public ObjectId Team { get; set; }
        public ObjectId[] Documents { get; set; }
    }
}
