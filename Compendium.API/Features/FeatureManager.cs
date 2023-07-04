using BetterCommands.Management;

using Compendium.Attributes;

using helpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Compendium.Features
{
    public static class FeatureManager
    {
        private static readonly List<Type> _knownFeatures = new List<Type>();
        private static readonly List<IFeature> _features = new List<IFeature>();

        public static readonly Type IFeatureInterfaceType = typeof(IFeature);

        public static string DirectoryPath => $"{Plugin.Handler.PluginDirectoryPath}/features";

        public static IReadOnlyList<IFeature> LoadedFeatures => _features;
        public static IReadOnlyList<Type> RegisteredFeatures => _knownFeatures;

        [InitOnLoad]
        public static void Reload()
        {
            Unload();

            foreach (var type in Assembly
                .GetExecutingAssembly()
                .GetTypes())
            {
                if (Reflection.HasInterface<IFeature>(type))
                {
                    _knownFeatures.Add(type);
                    _features.Add(Reflection.Instantiate<IFeature>(type));

                    Plugin.Info($"Loaded feature: {type.FullName}");
                }
            }

            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);

            Plugin.Info($"Loading features ..");

            foreach (var file in Directory.GetFiles(DirectoryPath, "*.dll"))
            {
                var rawAssembly = File.ReadAllBytes(file);
                var assembly = Assembly.Load(rawAssembly);

                foreach (var type in assembly.GetTypes())
                {
                    if (Reflection.HasInterface<IFeature>(type))
                    {
                        _knownFeatures.Add(type);
                        _features.Add(Reflection.Instantiate<IFeature>(type));

                        CommandManager.Register(type.Assembly);

                        Plugin.Info($"Loaded feature: {type.FullName}");
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

            feature.Load();
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

            feature.Unload();
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
            if (!Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name))
            {
                feature.Load();
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
            feature.Unload();
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
            _features.ForEach(x =>
            {
                if (!Plugin.Config.FeatureSettings.Disabled.Contains(x.Name))
                {
                    x.Load();
                }
            });
        }

        public static void Unload()
        {
            _features.ForEach(x =>
            {
                x.Unload();
            });

            _features.Clear();
            _knownFeatures.Clear();
        }
    }
}