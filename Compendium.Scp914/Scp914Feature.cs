using Compendium.Features;

using helpers.Configuration.Converters.Yaml;
using helpers.Configuration.Ini;

namespace Compendium.Scp914
{
    public class Scp914Feature : IFeature
    {
        private IniConfigHandler _config;

        public string Name => "SCP-914";

        public void Load()
        {
            new IniConfigBuilder()
                .WithConverter<YamlConfigConverter>()
                .WithGlobalPath($"{FeatureManager.DirectoryPath}/scp_914.ini")
                .WithType(typeof(Scp914Logic), null)
                .Register(ref _config);

            _config.Read();
            Scp914Logic.Load();
        }

        public void Reload()
        {
            _config?.Read();
        }

        public void Unload()
        {
            _config?.Save();
            Scp914Logic.Unload();
            _config = null;
        }
    }
}