namespace PDS_Server.Elements.Revit
{
    public class DbFamilyCategory : DbElement
    {
        public DbFamilyCategory() { }
        public string Name { get; set; }
        public string ImageData { get; set; }
        public DbFamilyCategory[] Children { get; set; }
    }
}
