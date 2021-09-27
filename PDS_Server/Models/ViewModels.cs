namespace PDS_Server.Models
{
    public class UserSettingsModel
    {
        public UserSettingsModel(int selected = 0) 
        {
            Selected = selected;
        }
        public int Selected { get; set; }
    }
    public class PluginModel
    {
        public PluginModel() { }
        public string Name { get; set; }
        public string Description { get; set; }
        public PluginVersionModel[] Versions { get; set; }
    }

    public class PluginVersionModel
    {
        public PluginVersionModel() { }
        public string Number { get; set; }
        public PluginRevitVersionModel[] RevitVersions { get; set; }
    }

    public class PluginRevitVersionModel
    {
        public PluginRevitVersionModel() { }
        public string Link { get; set; }
    }

    public class RedirectModel
    {
        public RedirectModel() { }
        public string Title { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
        public string Quote { get; set; }
        public string Sticker { get; set; } = "business/confirmed/010-chess game.svg";
        public string Action { get; set; } = "index";
        public string Controller { get; set; } = "home";
    }
}
