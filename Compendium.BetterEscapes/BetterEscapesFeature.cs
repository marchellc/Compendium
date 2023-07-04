using Compendium.Features;

using helpers.Configuration.Converters.Yaml;
using helpers.Configuration.Ini;

namespace Compendium.BetterEscapes
{
    public class BetterEscapesFeature : IFeature
    {
        private IniConfigHandler _config;

        public string Name => "Better Escapes";

        public void Load()
        {
            new IniConfigBuilder()
                .WithConverter<YamlConfigConverter>()
                .WithGlobalPath($"{FeatureManager.DirectoryPath}/better_escapes.ini")
                .WithType(typeof(BetterEscapesLogic), null)
                .Register(ref _config);

            _config.Read();
            BetterEscapesLogic.IsEnabled = true;
        }

        public void Reload()
        {
            _config?.Read();
        }

        public void Unload()
        {
            _config?.Save();
            BetterEscapesLogic.IsEnabled = false;
            _config = null;
        }
    }
}