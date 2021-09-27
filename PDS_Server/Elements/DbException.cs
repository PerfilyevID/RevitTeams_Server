namespace PDS_Server.Elements
{
    public class DbException : DbElement
    {
        public DbException() { }
        public string User { get; set; }
        public string Time { get; set; }
        public string Data { get; set; }
        public string Plugin { get; set; }
    }
}
