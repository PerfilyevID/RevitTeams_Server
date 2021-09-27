using Microsoft.Extensions.Configuration;
using System.IO;

namespace PDS_Server
{
    internal static class ConfigurationManager
    {
        internal static IConfiguration AppSetting { get; }
        static ConfigurationManager()
        {
            AppSetting = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("config.json")
                    .Build();
        }
    }
}
