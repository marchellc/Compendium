using Compendium.Events;
using Compendium.Logging;
using Compendium.Parsers;
using Compendium.Round;

using helpers;
using helpers.Patching;
using helpers.Events;
using helpers.Attributes;

using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Loader;

using BetterCommands;
using BetterCommands.Permissions;

using System;
using System.Reflection;

using Utils.NonAllocLINQ;

using Log = PluginAPI.Core.Log;

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

        [PluginConfig] 
        public Config ConfigInstance;
        public PluginHandler HandlerInstance;

        [PluginEntryPoint(
            "Compendium API",
            "3.3.2",
            "A huge API for each Compendium component.",
            "marchellc_")]
        [PluginPriority(PluginAPI.Enums.LoadPriority.Lowest)]
        public void Load()
        {
            if (Instance != null)
                throw new InvalidOperationException($"This plugin has already been loaded!");

            Instance = this;
            HandlerInstance = PluginHandler.Get(this);

            if (Config.UseLoggingProxy)
                helpers.Log.AddLogger<LoggingProxy>();

            PlayerDataRecordParser.Load();
            Directories.Load();

            Info("Loading ..");

            Calls.OnFalse(() =>
            {
                try
                {
                    var exec = Assembly.GetExecutingAssembly();

                    PatchManager.PatchAssemblies(exec);
                    EventRegistry.RegisterEvents(exec);
                    AttributeLoader.ExecuteLoadAttributes(exec);
                    RoundHelper.ScanAssemblyForOnChanged(exec);

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
            var exec = Assembly.GetExecutingAssembly();

            PatchManager.UnpatchAssemblies(exec);
            EventRegistry.UnregisterEvents(exec);
            AttributeLoader.ExecuteUnloadAttributes(exec);

            AssemblyLoader.Plugins.ForEachKey(pl =>
            {
                if (pl == exec)
                    return;

                PatchManager.UnpatchAssemblies(pl);
                EventRegistry.UnregisterEvents(pl);
                AttributeLoader.ExecuteUnloadAttributes(pl);
            });

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

            AttributeLoader.ExecuteReloadAttributes(Assembly.GetExecutingAssembly());
            AssemblyLoader.Plugins.ForEachKey(pl => AttributeLoader.ExecuteReloadAttributes(pl));

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
            helpers.Log.Debug(message);

            if (!Config.LogSettings.ShowDebug) 
                return;

            Log.Debug(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(1));
        }

        public static void Error(object message)
        {
            if (!Config.UseLoggingProxy)
                helpers.Log.Error(message);

            Log.Error(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(1));
        }

        public static void Warn(object message)
        {
            if (!Config.UseLoggingProxy)
                helpers.Log.Warn(message);

            Log.Warning(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(1));
        }

        public static void Info(object message)
        {
            if (!Config.UseLoggingProxy)
                helpers.Log.Info(message);

            Log.Info(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(1));
        }

        [Command("announcerestart", CommandType.GameConsole, CommandType.RemoteAdmin, CommandType.PlayerConsole)]
        [Permission(PermissionLevel.Administrator)]
        [CommandAliases("ar")]
        [Description("Announces a server restart and then restarts in 10 seconds.")]
        private static string AnnounceRestartCommand(ReferenceHub sender)
        {
            World.Broadcast($"<color=red><b>Server se restartuje za 10 sekund!</b></color>", 10, true);
            Calls.Delay(10f, () => Server.Restart());

            return "Restarting in 10 seconds ..";
        }

        [Command("creload", CommandType.GameConsole, CommandType.RemoteAdmin)]
        [Description("Reloads Compendium's core API.")]
        [Permission(PermissionLevel.Administrator)]
        private static string ReloadCommand(ReferenceHub sender)
        {
            if (Instance is null)
                return "Instance is inactive.";

            Instance.Reload();
            return "Reloaded!";
        }
    }
}