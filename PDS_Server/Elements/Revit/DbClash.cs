namespace PDS_Server.Elements.Revit
{
    public class DbClash
    {
        public DbClash() { }
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageData { get; set; }
        public string Element_A { get; set; }
        public string Element_B { get; set; }
        public int Element_A_Id { get; set; }
        public int Element_B_Id { get; set; }
        public DbPoint Point { get; set; }
        public int Status { get; set; } = -1;
        public int GroupId { get; set; } = -1;
        public string ChatId { get; set; }
    }
}
