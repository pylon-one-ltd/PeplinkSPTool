using Microsoft.Extensions.Configuration;

namespace PeplinkSPTool.Classes
{
    /// <summary>
    /// Helpers for executable initialisation
    /// </summary>
    public static class InitialisationHandler
    {
        /// <summary>
        /// Find the configuration file for the executable
        /// </summary>
        /// <param name="Arguments">Arguments parsed during launch</param>
        /// <returns>iConfigurationBuilder if valid, or null if not found</returns>
        public static IConfiguration? LoadConfiguration(IEnumerable<string> Arguments)
        {
            string? ApplicationConfigFile = null;
            
            // Configuration File Command Argument
            var ArgumentSearch = Arguments.FirstOrDefault(q => q.Contains("--config=", StringComparison.CurrentCultureIgnoreCase));
            if( !string.IsNullOrEmpty(ArgumentSearch) )
            {
                var ConfigurationFile = ArgumentSearch.Split('=')[1];
                if( ConfigurationFile.EndsWith(".json") && File.Exists(ConfigurationFile) )
                {
                    Console.WriteLine($"Configuration file loaded from path: '{ConfigurationFile}");
                    ApplicationConfigFile = ConfigurationFile;
                }
            }
            
            // Configuration File Discovery
            var ExecutablePath = Path.GetDirectoryName(AppContext.BaseDirectory)?.Replace("file:\\", "").Replace("file:", "");
            if( ApplicationConfigFile == null && ExecutablePath != null )
            {
                // First check the parent directory for a global configuration file
                var GlobalAppConfig = Path.Combine(Directory.GetParent(ExecutablePath)?.FullName ?? "", "appsettings.json");
                if( File.Exists(GlobalAppConfig) )
                {
                    Console.WriteLine($"Configuration file loaded from path: '{GlobalAppConfig}");
                    ApplicationConfigFile = GlobalAppConfig;
                }
                
                // Finally check the current directory (standard asp.net core implementation)
                var ProjectAppConfig = Path.Combine(ExecutablePath, "appsettings.json");
                if( File.Exists(ProjectAppConfig) )
                {
                    Console.WriteLine($"Configuration file loaded from path: '{ProjectAppConfig}");
                    ApplicationConfigFile = ProjectAppConfig;
                }
            }
            
            // Return the configuration file path
            if( ApplicationConfigFile == null ) return null;
            return new ConfigurationBuilder().AddJsonFile(ApplicationConfigFile, false, true)
            .AddJsonFile(ApplicationConfigFile.Replace(".json", ".development.json"), true, true).Build();
        }
    }
}