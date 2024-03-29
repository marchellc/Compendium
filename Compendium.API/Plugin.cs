﻿using Compendium.Events;
using Compendium.Logging;
using Compendium.Custom.Parsers;
using Compendium.Features;

using helpers;
using helpers.Patching;
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

using Compendium.Attributes;
using Compendium.Updating;

namespace Compendium
{
    [LogSource("Compendium Core")]
    public class Plugin
    {
        public static Plugin Instance { get; private set; }
        public static Config Config { get => Instance?.ConfigInstance ?? null; }
        public static PluginHandler Handler { get => Instance?.HandlerInstance ?? null; }

        [PluginConfig] 
        public Config ConfigInstance;
        public PluginHandler HandlerInstance;

        [PluginEntryPoint(
            "Compendium API",
            "3.8.4",
            "A huge API for each Compendium component.",
            "marchellc_")]
        [PluginPriority(PluginAPI.Enums.LoadPriority.Lowest)]
        public void Load()
        {
            if (Instance != null)
                throw new InvalidOperationException($"This plugin has already been loaded!");

            Instance = this;
            HandlerInstance = PluginHandler.Get(this);

            if (Config.LogSettings.UseLoggingProxy)
                helpers.Log.AddLogger<LoggingProxy>();

            PlayerDataRecordParser.Load();
            StaffGroupParser.Load();
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

                    AttributeRegistry<RoundStateChangedAttribute>.Register();

                    UpdateHandler.Register();

                    helpers.Log.AddLogger(new helpers.Logging.Loggers.FileLogger(helpers.Logging.Loggers.FileLoggerMode.AppendToFile, 0, $"Server {ServerStatic.ServerPort}.txt"));

                    LoadConfig();
                    Info("Loaded!");
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

            UpdateHandler.Unregister();

            AssemblyLoader.Plugins.ForEachKey(pl =>
            {
                if (pl == exec)
                    return;

                PatchManager.UnpatchAssemblies(pl);
                EventRegistry.UnregisterEvents(pl);
                AttributeLoader.ExecuteUnloadAttributes(pl);
                UpdateHandler.Unregister(pl);
            });

            SaveConfig();

            Instance = null;
            HandlerInstance = null;
            ConfigInstance = null;
        }

        [PluginReload]
        public void Reload()
        {
            LoadConfig();
            AttributeLoader.ExecuteReloadAttributes(Assembly.GetExecutingAssembly());
            FeatureManager.LoadedFeatures.ForEach(f => AttributeLoader.ExecuteReloadAttributes(f.GetType().Assembly));
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

            Log.Debug(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(3));
        }

        public static void Error(object message)
        {
            if (!Config.LogSettings.UseLoggingProxy)
                helpers.Log.Error(message);

            Log.Error(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(3));
        }

        public static void Warn(object message)
        {
            if (!Config.LogSettings.UseLoggingProxy)
                helpers.Log.Warn(message);

            Log.Warning(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(3));
        }

        public static void Info(object message)
        {
            if (!Config.LogSettings.UseLoggingProxy)
                helpers.Log.Info(message);

            Log.Info(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller(3));
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