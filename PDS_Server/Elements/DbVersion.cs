namespace PDS_Server.Elements
{
    public class DbVersion
    {
        public DbVersion() { }
        public string Number { get; set; }
        public string Changelog { get; set; }
        public bool Published { get; set; } = false;
        public DbRevitVersionInstance[] RevitVersions { get; set; } = new DbRevitVersionInstance[]
        {
            new DbRevitVersionInstance() { Number = "2016" },
            new DbRevitVersionInstance() { Number = "2017" },
            new DbRevitVersionInstance() { Number = "2018" },
            new DbRevitVersionInstance() { Number = "2019" },
            new DbRevitVersionInstance() { Number = "2020" },
            new DbRevitVersionInstance() { Number = "2021" }
        };
    }
}
