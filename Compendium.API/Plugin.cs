using Compendium.Attributes;
using Compendium.Helpers.Events;

using helpers;
using helpers.Events;
using helpers.Extensions;

using MEC;

using PluginAPI.Enums;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Log = PluginAPI.Core.Log;
using Compendium.Helpers.Staff;
using Compendium.Helpers.Calls;
using Compendium.Logging;

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
            "1.0.0",
            "A huge API for each Compendium component.",
            "marchellc_")]
        [PluginPriority(LoadPriority.Lowest)]
        public void Load()
        {
            if (Instance != null)
                throw new InvalidOperationException($"This plugin has already been loaded!");

            Info("Loading ..");

            Instance = this;
            HandlerInstance = PluginHandler.Get(this);

            EventConverter.Initialize();

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
                helpers.Log.ClearLevelBlacklist();
                helpers.Log.ClearSourceBlacklist();

                Warn($"Support library logger enabled - this might get messy!");
            }

            if (Config.LogSettings.ShowDebug)
            {
                Warn($"Debug enabled - this will get messy.");
            }

            CallHelper.CallWhenFalse(() =>
            {
                var initMethods = new List<Tuple<MethodInfo, int>>();

                foreach (var type in Assembly
                    .GetExecutingAssembly()
                    .GetTypes())
                {
                    foreach (var method in type.GetMethods())
                    {
                        if (method.TryGetAttribute<InitOnLoadAttribute>(out var attributeValue))
                        {
                            initMethods.Add(new Tuple<MethodInfo, int>(method, attributeValue.Priority < 0 ? 0 : attributeValue.Priority));
                        }
                    }
                }

                initMethods
                   .OrderByDescending(x => x.Item2)
                   .ForEach(y =>
                   {
                       try
                       {
                           y.Item1.Invoke(null, null);
                       }
                       catch (Exception ex)
                       {
                           Error($"Failed to invoke InitOnLoad method {y.Item1.ToLogName(false)}!");
                           Error(ex);
                       }
                   });

                LoadConfig();

                Info("Loaded!");

                OnLoaded.Invoke();
            }, () => ServerStatic.PermissionsHandler is null);
        }

        [PluginUnload]
        public void Unload()
        {
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
            OnReloaded.Invoke();
        }

        public static void SaveConfig()
        {
            Handler?.SaveConfig(Instance, nameof(ConfigInstance));
            StaffHelper.SaveStaff();
        }

        public static void LoadConfig()
        {
            Handler?.LoadConfig(Instance, nameof(ConfigInstance));
            StaffHelper.LoadStaff();
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