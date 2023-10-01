using PluginAPI.Helpers;

using System.IO;

namespace Compendium
{
    public static class Directories
    {
        public static string MainPath => $"{Paths.SecretLab}/compendium";

        public static string DataPath => $"{MainPath}/data";
        public static string FeaturesPath => $"{MainPath}/features";
        public static string ConfigPath => $"{MainPath}/configs";

        public static string ThisConfigs => !Plugin.Config.ApiSetttings.GlobalDirectories.Contains("config") ? $"{MainPath}/configs_{ServerStatic.ServerPort}" : ConfigPath;
        public static string ThisData => !Plugin.Config.ApiSetttings.GlobalDirectories.Contains("data") ? $"{MainPath}/data_{ServerStatic.ServerPort}" : DataPath;
        public static string ThisFeatures => !Plugin.Config.ApiSetttings.GlobalDirectories.Contains("features") ? $"{MainPath}/features_{ServerStatic.ServerPort}" : FeaturesPath;

        public static string GetDataPath(string fileName, string dataId = null, bool useGlobal = true)
        {
            var path = "";

            if (!string.IsNullOrWhiteSpace(dataId))
            {
                if (Plugin.Config.ApiSetttings.GlobalDirectories.Contains(dataId))
                    path = $"{DataPath}/{fileName}";
                else
                    path = useGlobal ? $"{ThisData}/{fileName}" : $"{MainPath}/data_{ServerStatic.ServerPort}/{fileName}";
            }
            else
            {
                path = $"{ThisData}/{fileName}";
            }

            var dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return path;
        }

        internal static void Load()
        {
            Plugin.Info($"Loading directories ..");

            Plugin.Info($"Main directory: {MainPath}");
            Plugin.Info($"Config directory: {ThisConfigs}");
            Plugin.Info($"Data directory: {ThisData}");
            Plugin.Info($"Features directory: {ThisFeatures}");

            if (!Directory.Exists(MainPath))
                Directory.CreateDirectory(MainPath);

            if (!Directory.Exists(ThisConfigs))
                Directory.CreateDirectory(ThisConfigs);

            if (!Directory.Exists(ThisData))
                Directory.CreateDirectory(ThisData);

            if (!Directory.Exists(ThisFeatures))
                Directory.CreateDirectory(ThisFeatures);
        }
    }
}