using Compendium.Features;

using helpers.Configuration.Converters.Yaml;
using helpers.Configuration.Ini;

namespace Compendium.Voice
{
    public class VoiceFeature : IFeature
    {
        private IniConfigHandler _config;

        public string Name => "Voice";

        public void Load()
        {
            new IniConfigBuilder()
                .WithConverter<YamlConfigConverter>()
                .WithGlobalPath($"{FeatureManager.DirectoryPath}/voice.ini")
                .WithType(typeof(VoiceConfigs), null)
                .Register(ref _config);

            _config.Read();

            VoiceController.Load();
        }

        public void Reload()
        {
            _config?.Read();
        }

        public void Unload()
        {
            _config?.Save();
            _config = null;
            VoiceController.Unload();
        }
    }
}