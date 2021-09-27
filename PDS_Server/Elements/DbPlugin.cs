namespace PDS_Server.Elements
{
    public class DbPlugin : DbElement
    {
        public DbPlugin() { }
        public DbVersion[] Versions { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Values: cmn, tms, pnl.
        /// </summary>
        public string Target { get; set; }
        public string Description { get; set; }
    }
}
