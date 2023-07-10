using helpers.Configuration.Converters.Yaml;
using helpers.Configuration.Ini;
using helpers.Events;

namespace Compendium.Features
{
    public class ConfigFeatureBase : IFeature
    {
        private bool _isEnabled;

        public virtual string Name => "Feature Base";
        public virtual bool IsPatch => false;

        public bool IsEnabled => _isEnabled;

        public IniConfigHandler Config { get; private set; }

        public readonly EventProvider OnLoad = new EventProvider();
        public readonly EventProvider OnUnload = new EventProvider();
        public readonly EventProvider OnReload = new EventProvider();
        public readonly EventProvider OnUpdate = new EventProvider();
        public readonly EventProvider OnRestart = new EventProvider();
        public readonly EventProvider OnWaitingForPlayers = new EventProvider();

        public virtual void CallUpdate()
        {
            OnUpdate.Invoke();
        }

        public virtual void Load()
        {
            _isEnabled = true;

            LoadConfig();

            OnLoad.Invoke();
        }

        public virtual void Reload()
        {
            Config?.Read();
            OnReload.Invoke();
        }

        public virtual void Restart()
        {
            OnRestart.Invoke();
        }

        public virtual void OnWaiting()
        {
            Config?.Read();
            OnWaitingForPlayers.Invoke();
        }

        public virtual void Unload()
        {
            _isEnabled = false;

            OnUnload.Invoke();

            Config?.Save();
            Config = null;
        }

        public void SaveConfig()
            => Config?.Save();

        public void LoadConfig()
        {
            if (Config is null)
            {
                Config = new IniConfigBuilder()
                    .WithConverter<YamlConfigConverter>()
                    .WithWatcher()
                    .WithNamingRule(helpers.Configuration.ConfigNamingRule.SetValue)
                    .WithAssembly(GetType().Assembly)
                    .WithPath($"{FeatureManager.DirectoryPath}/{Name}_config.ini")
                    .Build();
            }

            Config.Read();
        }
    }
}