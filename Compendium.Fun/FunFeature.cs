using Compendium.Features;

using helpers.Configuration.Converters.Yaml;
using helpers.Configuration.Ini;

namespace Compendium.Fun
{
    public class FunFeature : IFeature
    {
        private IniConfigHandler _config;

        public string Name => "Fun";

        public void Load()
        {
            new IniConfigBuilder()
                .WithConverter<YamlConfigConverter>()
                .WithGlobalPath($"{FeatureManager.DirectoryPath}/fun.ini")
                .WithType(typeof(FunConfig), null)
                .Register(ref _config);

            _config.Read();
        }

        public void Reload()
        {
            _config?.Read();
        }

        public void Unload()
        {
            _config?.Save();
            _config = null;
        }
    }
}