using BetterCommands;
using BetterCommands.Management;
using BetterCommands.Permissions;

using Compendium.Helpers.Events;
using Compendium.Helpers.Round;
using helpers;
using helpers.Attributes;
using helpers.Patching;

using PluginAPI.Core;
using PluginAPI.Enums;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Compendium.Features
{
    public static class FeatureManager
    {
        private static bool _handlerAdded;
        private static bool _pauseUpdate;

        private static readonly List<Type> _knownFeatures = new List<Type>();
        private static readonly List<IFeature> _features = new List<IFeature>();

        public static string DirectoryPath => $"{Plugin.Handler.PluginDirectoryPath}/features";

        public static IReadOnlyList<IFeature> LoadedFeatures => _features;
        public static IReadOnlyList<Type> RegisteredFeatures => _knownFeatures;

        [Load]
        [Reload]
        public static void Reload()
        {
            Unload();

            foreach (var type in Assembly
                .GetExecutingAssembly()
                .GetTypes())
            {
                if (type == typeof(FeatureBase) || type == typeof(ConfigFeatureBase))
                    continue;

                if (Reflection.HasInterface<IFeature>(type))
                {
                    _knownFeatures.Add(type);

                    var instance = Reflection.Instantiate<IFeature>(type);

                    _features.Add(instance);

                    Singleton.Set(instance);

                    Plugin.Info($"Instantiated internal feature: {type.FullName}");
                }
            }

            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);

            Plugin.Info($"Loading external features ..");

            foreach (var file in Directory.GetFiles(DirectoryPath, "*.dll"))
            {
                var rawAssembly = File.ReadAllBytes(file);
                var assembly = Assembly.Load(rawAssembly);

                foreach (var type in assembly.GetTypes())
                {
                    if (type == typeof(FeatureBase) || type == typeof(ConfigFeatureBase))
                        continue;

                    if (Reflection.HasInterface<IFeature>(type))
                    {
                        _knownFeatures.Add(type);

                        var instance = Reflection.Instantiate<IFeature>(type);

                        _features.Add(instance);

                        Singleton.Set(instance);
                        CommandManager.Register(type.Assembly);
                        RoundHelper.ScanAssemblyForOnChanged(assembly);

                        Plugin.Info($"Instantiated external feature: {type.FullName}");
                    }
                }
            }

            Plugin.Info($"Loaded {_features.Count} features!");

            Load();
        }

        public static bool IsRegistered<TFeature>() where TFeature : IFeature => _knownFeatures.Contains(typeof(TFeature));
        public static bool IsRegistered(Type type) => _knownFeatures.Contains(type);
        public static bool IsInstantiated(Type type) => TryGetFeature(type, out _);
        public static bool IsInstantiated<TFeature>() where TFeature : IFeature => TryGetFeature<TFeature>(out _);

        public static void Enable(string name) { if (TryGetFeature(name, out var feature)) Enable(feature); }
        public static void Enable<TFeature>() where TFeature : IFeature { if (TryGetFeature<TFeature>(out var feature)) Enable(feature); }
        public static void Enable(Type type) { if (TryGetFeature(type, out var feature)) Enable(feature); }
        public static void Enable(IFeature feature)
        {
            if (Plugin.Config.FeatureSettings.Disabled.Remove(feature.Name))
                Plugin.SaveConfig();

            Load(feature);
        }

        public static void Disable(string name) { if (TryGetFeature(name, out var feature)) Disable(feature); }
        public static void Disable<TFeature>() where TFeature : IFeature { if (TryGetFeature<TFeature>(out var feature)) Disable(feature); }
        public static void Disable(Type type) { if (TryGetFeature(type, out var feature)) Disable(feature); }
        public static void Disable(IFeature feature)
        {
            if (!Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name))
            {
                Plugin.Config.FeatureSettings.Disabled.Add(feature.Name);
                Plugin.SaveConfig();
            }

            Unload(feature);
        }

        public static void Load<TFeature>() where TFeature : IFeature
        {
            if (TryGetFeature<TFeature>(out var feature))
            {
                Load(feature);
            }
        }

        public static void Load(Type type)
        {
            if (TryGetFeature(type, out var feature))
            {
                Load(feature);
            }
        }

        public static void Load(string name)
        {
            if (TryGetFeature(name, out var feature))
            {
                Load(feature);
            }
        }

        public static void Load(IFeature feature)
        {
            try
            {
                if (!Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name))
                {
                    feature.Load();

                    if (feature.IsPatch)
                        PatchManager.PatchAssemblies(feature.GetType().Assembly);

                    AttributeLoader.ExecuteLoadAttributes(feature.GetType().Assembly);
                }
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to load feature {feature.Name}:\n{ex}");
            }
        }

        public static void Unload<TFeature>() where TFeature : IFeature
        {
            if (TryGetFeature<TFeature>(out var feature))
            {
                Unload(feature);
            }
        }

        public static void Unload(Type type)
        {
            if (TryGetFeature(type, out var feature))
            {
                Unload(feature);
            }
        }

        public static void Unload(string name)
        {
            if (TryGetFeature(name, out var feature))
            {
                Unload(feature);
            }
        }

        public static void Unload(IFeature feature)
        {
            try
            {
                AttributeLoader.ExecuteUnloadAttributes(feature.GetType().Assembly);

                if (feature.IsPatch)
                    PatchManager.UnpatchAssemblies(feature.GetType().Assembly);

                feature.Unload();
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to unload feature {feature.Name}:\n{ex}");
            }
        }

        public static void Register<TFeature>() where TFeature : IFeature => Register(typeof(TFeature));

        public static void Register(Type type)
        {
            if (!_knownFeatures.Contains(type))
            {
                _knownFeatures.Add(type);
                _features.Add(Reflection.Instantiate<IFeature>(type));
            }
        }

        public static void Unregister<TFeature>() where TFeature : IFeature => Unregister(typeof(TFeature));

        public static void Unregister(IFeature feature)
        {
            if (_knownFeatures.Remove(feature.GetType()))
            {
                _features.Remove(feature);
            }
        }

        public static void Unregister(string name)
        {
            if (TryGetFeature(name, out var feature))
            {
                Unregister(feature);
            }
        }

        public static void Unregister(Type type)
        {
            if (_knownFeatures.Contains(type))
            {
                _knownFeatures.Remove(type);
                _features.Remove(_features.First(x => x.GetType() == type));
            }
        }

        public static IFeature GetFeature(string name) => TryGetFeature(name, out var feature) ? feature : null;
        public static TFeature GetFeature<TFeature>() where TFeature : IFeature => TryGetFeature<TFeature>(out var feature) ? feature : default;
        public static IFeature GetFeature(Type type) => TryGetFeature(type, out var feature) ? feature : null;

        public static bool TryGetFeature(string name, out IFeature feature)
        {
            feature = _features.FirstOrDefault(x => x.Name == name);
            return feature != null;
        }

        public static bool TryGetFeature<TFeature>(out TFeature feature) where TFeature : IFeature
        {
            feature = _features.FirstOrDefault(x => x is TFeature).As<TFeature>();
            return feature != null;
        }

        public static bool TryGetFeature(Type type, out IFeature feature)
        {
            feature = _features.FirstOrDefault(x => x.GetType() == type);
            return feature != null;
        }

        public static void Load()
        {
            _features.ForEach(Load);

            ServerEventType.RoundRestart.AddHandler<Action>(OnRestart);
            ServerEventType.WaitingForPlayers.AddHandler<Action>(OnWaiting);

            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", OnUpdate);

            _handlerAdded = true;
        }

        public static void Unload()
        {
            if (_handlerAdded)
            {
                ServerEventType.RoundRestart.RemoveHandler<Action>(OnRestart);
                ServerEventType.WaitingForPlayers.RemoveHandler<Action>(OnWaiting);

                Reflection.TryRemoveHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", OnUpdate);

                _handlerAdded = false;
            }

            _features.ForEach(Unload);
            _features.Clear();
            _knownFeatures.Clear();
        }

        private static void OnWaiting()
        {
            _features.ForEach(feature =>
            {
                try
                {
                    if (!feature.IsEnabled)
                        return;

                    feature.OnWaiting();
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed to invoke the OnWaiting function of feature {feature.Name}:\n{ex}");
                }
            });
            _pauseUpdate = false;
        }

        private static void OnRestart()
        {
            _pauseUpdate = true;
            _features.ForEach(feature =>
            {
                try
                {
                    if (!feature.IsEnabled)
                        return;

                    feature.Restart();
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed to invoke the Restart function of feature {feature.Name}:\n{ex}");
                }
            });
        }

        private static void OnUpdate()
        {
            if (_pauseUpdate)
                return;

            _features.ForEach(feature =>
            {
                try
                {
                    if (!feature.IsEnabled)
                        return;

                    feature.CallUpdate();
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed to invoke the Update function of feature {feature.Name}:\n{ex}");
                }
            });
        }

        [Command("disablefeature", BetterCommands.CommandType.RemoteAdmin, BetterCommands.CommandType.GameConsole)]
        [CommandAliases("dfeature", "disablef")]
        [Permission(PermissionLevel.Administrator)]
        private static string DisableFeature(Player sender, string featureName)
        {
            if (TryGetFeature(featureName, out var feature))
            {
                if (!feature.IsEnabled)
                {
                    return $"Feature {feature.Name} is already disabled!";
                }
                else
                {
                    Disable(feature);
                    return $"Feature {feature.Name} has been disabled!";
                }
            }
            else
            {
                return $"Feature {featureName} does not exist!";
            }
        }

        [Command("enablefeature", BetterCommands.CommandType.RemoteAdmin, BetterCommands.CommandType.GameConsole)]
        [CommandAliases("efeature", "enablef")]
        [Permission(PermissionLevel.Administrator)]
        private static string EnableFeature(Player sender, string featureName)
        {
            if (TryGetFeature(featureName, out var feature))
            {
                if (feature.IsEnabled)
                {
                    return $"Feature {feature.Name} is already enabled!";
                }
                else
                {
                    Enable(feature);
                    return $"Feature {feature.Name} has been enabled!";
                }
            }
            else
            {
                return $"Feature {featureName} does not exist!";
            }
        }

        [Command("reloadfeature", BetterCommands.CommandType.RemoteAdmin, BetterCommands.CommandType.GameConsole)]
        [CommandAliases("rfeature", "reloadf")]
        [Permission(PermissionLevel.Administrator)]
        private static string ReloadFeature(Player sender, string featureName)
        {
            if (TryGetFeature(featureName, out var feature))
            {
                if (!feature.IsEnabled)
                {
                    return $"Feature {feature.Name} is disabled!";
                }
                else
                {
                    AttributeLoader.ExecuteReloadAttributes(feature.GetType().Assembly);
                    feature.Reload();
                    return $"Feature {feature.Name} has been reloaded!";
                }
            }
            else
            {
                return $"Feature {featureName} does not exist!";
            }
        }
    }
}