using Compendium.Features;

using helpers.Configuration.Converters.Yaml;
using helpers.Configuration.Ini;

namespace Compendium.BetterTesla
{
    public class BetterTeslaFeature : IFeature
    {
        private static IniConfigHandler _config;

        public string Name => "Better Tesla";

        public void Load()
        {
            new IniConfigBuilder()
                .WithConverter<YamlConfigConverter>()
                .WithGlobalPath($"{FeatureManager.DirectoryPath}/better_tesla.ini")
                .WithType(typeof(BetterTeslaLogic), null)
                .Register(ref _config);

            _config.Read();
            BetterTeslaLogic.IsEnabled = true;
        }

        public void Reload()
        {
            _config?.Read();
        }

        public void Unload()
        {
            _config.Save();
            BetterTeslaLogic.IsEnabled = false;
            _config = null;
        }
    }
}