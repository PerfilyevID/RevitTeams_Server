namespace PDS_Server.Elements
{
    public abstract class DbElement
    {
        public MongoDB.Bson.ObjectId Id { get; set; }
    }
}
