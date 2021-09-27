using MongoDB.Bson;

namespace PDS_Server.Elements.Revit
{
    public class DbFamily : DbElement
    {
        public DbFamily() { }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public string ImageData { get; set; }
        public string GltfData { get; set; }
        public string LOD { get; set; }
        public ObjectId Team { get; set; }
        public ObjectId Category { get; set; }
    }
}
