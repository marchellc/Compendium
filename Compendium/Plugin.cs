using Compendium.Attributes;

using helpers;
using helpers.Events;
using helpers.Extensions;
using helpers.Translations;

using MEC;

using PluginAPI.Core;
using PluginAPI.Core.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Log = PluginAPI.Core.Log;

namespace Compendium
{
    [LogSource("Compendium Loader")]
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
            "Compendium",
            "1.0.0",
            "A compendium of other plugins and useful features.",
            "fleccker")]
        public void Load()
        {
            if (Instance != null) 
                throw new InvalidOperationException($"This plugin has already been loaded!");

            Info("Loading ..");

            Instance = this;
            HandlerInstance = PluginHandler.Get(this);

            Timing.RunCoroutine(Startup());
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
            Translator.Save();
        }

        public static void LoadConfig()
        {
            Handler?.LoadConfig(Instance, nameof(ConfigInstance));
            Translator.Load();
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

            Log.Debug(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller());
        }

        public static void Error(object message) => Log.Error(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller());
        public static void Warn(object message) => Log.Warning(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller());
        public static void Info(object message) => Log.Info(message?.ToString() ?? "Null Message!", helpers.Log.ResolveCaller());

        private static IEnumerator<float> Startup()
        {
            yield return Timing.WaitUntilTrue(() => ServerStatic.PermissionsHandler != null);

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

            Translations.Translation.RegisterAll();
            Translator.Load();

            Info("Loaded!");

            OnLoaded.Invoke();
        }
    }
}