using Compendium.Events;

using helpers;
using helpers.Events;
using helpers.Attributes;
using helpers.Logging.Loggers;

using PluginAPI.Core;
using PluginAPI.Core.Attributes;

using System;
using System.Reflection;

using Log = PluginAPI.Core.Log;

using Compendium.Calls;
using Compendium.Logging;
using Compendium.Features;
using helpers.Patching;

namespace Compendium
{
    [LogSource("Compendium")]
    public class Plugin
    {
        public static readonly EventProvider OnLoaded = new EventProvider();
        public static readonly EventProvider OnUnloaded = new EventProvider();
        public static readonly EventProvider OnReloaded = new EventProvider();

        public static Plugin Instance { get; private set; }
        public static Config Config { get => Instance?.ConfigInstance ?? null; }
        public static PluginHandler Handler { get => Instance?.HandlerInstance ?? null; }

        [PluginConfig] public Config ConfigInstance;
        public PluginHandler HandlerInstance;

        [PluginEntryPoint(
            "Compendium API",
            "2.0.0",
            "A huge API for each Compendium component.",
            "marchellc_")]
        [PluginPriority(PluginAPI.Enums.LoadPriority.Lowest)]
        public void Load()
        {
            if (Instance != null)
                throw new InvalidOperationException($"This plugin has already been loaded!");

            Info("Loading ..");

            Instance = this;
            HandlerInstance = PluginHandler.Get(this);

            if (Config.UseExceptionHandler)
            {
                ExceptionManager.Load();

                ExceptionManager.LogToConsole = true;
                ExceptionManager.LogPath = $"{Handler.PluginDirectoryPath}/exceptions.txt";
                ExceptionManager.LogAll = true;

                ExceptionManager.UnhandledLogAll = true;
                ExceptionManager.UnhandledLogPath = $"{Handler.PluginDirectoryPath}/unhandled_exceptions.txt";

                Info($"Exception handler is enabled.");
            }

            if (Config.UseLoggingProxy)
            {
                helpers.Log.AddLogger<LoggingProxy>();
                helpers.Log.AddLogger(new FileLogger(FileLoggerMode.AppendToFile, 0));
                helpers.Log.Blacklist(LogLevel.Debug);

                Warn($"Support library logger enabled - this might get messy!");
            }

            if (Config.LogSettings.ShowDebug)
            {
                helpers.Log.Unblacklist(LogLevel.Debug);

                Warn($"Debug enabled - this will get messy.");
            }

            CallHelper.CallWhenFalse(() =>
            {
                try
                {
                    var exec = Assembly.GetExecutingAssembly();

                    PatchManager.PatchAssemblies(exec);
                    AttributeLoader.ExecuteLoadAttributes(exec);
                    EventRegistry.RegisterEvents(exec);

                    LoadConfig();

                    Info("Loaded!");

                    OnLoaded.Invoke();
                }
                catch (Exception ex)
                {
                    Error($"Startup failed!");
                    Error(ex);
                }
            }, () => ServerStatic.PermissionsHandler is null);
        }

        [PluginUnload]
        public void Unload()
        {
            AttributeLoader.ExecuteUnloadAttributes();

            var exec = Assembly.GetExecutingAssembly();

            PatchManager.UnpatchAssemblies(exec);
            EventRegistry.UnregisterEvents(exec);

            SaveConfig();

            OnUnloaded.Invoke();

            Instance = null;
            HandlerInstance = null;
            ConfigInstance = null;
        }

        [PluginReload]
        public void Reload()
        {
            LoadConfig();

            AttributeLoader.ExecuteReloadAttributes();

            OnReloaded.Invoke();
        }

        public static void SaveConfig()
        {
            Handler?.SaveConfig(Instance, nameof(ConfigInstance));
        }

        public static void LoadConfig()
        {
            Handler?.LoadConfig(Instance, nameof(ConfigInstance));
        }
        
        public static void ModifyConfig(Action<Config> action)
        {
            action?.Invoke(Config);
            SaveConfig();
        }

        public static void Debug(object message)
        {
            if (!Config.LogSettings.ShowDebug) 
                return;

            Log.Debug(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(1));
        }

        public static void Error(object message) => Log.Error(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(1));
        public static void Warn(object message) => Log.Warning(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(1));
        public static void Info(object message) => Log.Info(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(1));
    }
}