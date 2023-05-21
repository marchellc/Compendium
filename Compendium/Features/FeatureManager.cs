using Compendium.Attributes;

using helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Compendium.Features
{
    public static class FeatureManager
    {
        private static readonly List<Type> _knownFeatures = new List<Type>();
        private static readonly List<IFeature> _features = new List<IFeature>();

        public static readonly Type IFeatureInterfaceType = typeof(IFeature);

        [InitOnLoad]
        public static void Initialize()
        {
            foreach (var type in Assembly
                .GetExecutingAssembly()
                .GetTypes())
            {
                if (type.IsSubclassOf(IFeatureInterfaceType))
                {
                    _knownFeatures.Add(type);
                    _features.Add(Reflection.Instantiate<IFeature>(type));
                }
            }

            Plugin.OnLoaded.Add(Load);
            Plugin.OnUnloaded.Add(Unload);
            Plugin.OnReloaded.Add(Reload);
        }

        public static bool IsRegistered<TFeature>() where TFeature : IFeature => _knownFeatures.Contains(typeof(TFeature));
        public static bool IsRegistered(Type type) => _knownFeatures.Contains(type);
        public static bool IsInstantiated(Type type) => TryGetFeature(type, out _);
        public static bool IsInstantiated<TFeature>() where TFeature : IFeature => TryGetFeature<TFeature>(out _);
        public static bool IsEnabled<TFeature>() where TFeature : IFeature => TryGetFeature<TFeature>(out var feature) && !feature.IsDisabled;
        public static bool IsRunning<TFeature>() where TFeature : IFeature => TryGetFeature<TFeature>(out var feature) && feature.IsRunning;
        public static bool IsRunningAndEnabled<TFeature>() where TFeature : IFeature => TryGetFeature<TFeature>(out var feature) && !feature.IsDisabled && feature.IsRunning;

        public static void Enable(string name) { if (TryGetFeature(name, out var feature)) Enable(feature); }
        public static void Enable<TFeature>() where TFeature : IFeature { if (TryGetFeature<TFeature>(out var feature)) Enable(feature); }
        public static void Enable(Type type) { if (TryGetFeature(type, out var feature)) Enable(feature); }
        public static void Enable(IFeature feature)
        {
            Verify(feature);
            if (Plugin.Config.FeatureSettings.Disabled.Remove(feature.Name)) Plugin.SaveConfig();
            feature.Enable();
            if (!feature.IsRunning) feature.Load();
        }

        public static void Disable(string name) { if (TryGetFeature(name, out var feature)) Disable(feature); }
        public static void Disable<TFeature>() where TFeature : IFeature { if (TryGetFeature<TFeature>(out var feature)) Disable(feature); }
        public static void Disable(Type type) { if (TryGetFeature(type, out var feature)) Disable(feature); }
        public static void Disable(IFeature feature)
        {
            Verify(feature);

            if (!Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name))
            {
                Plugin.Config.FeatureSettings.Disabled.Add(feature.Name);
                Plugin.SaveConfig();
            }

            feature.Disable();
            feature.Unload();
        }

        public static void Verify<TFeature>() where TFeature : IFeature
        {
            if (TryGetFeature<TFeature>(out var feature))
            {
                var isInConfig = Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name);
                var isDisabled = feature.IsDisabled;

                if (isDisabled && !isInConfig) feature.Enable();
                if (!isDisabled && isInConfig) feature.Disable();
            }
        }

        public static void Verify(string name)
        {
            if (TryGetFeature(name, out var feature))
            {
                var isInConfig = Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name);
                var isDisabled = feature.IsDisabled;

                if (isDisabled && !isInConfig) feature.Enable();
                if (!isDisabled && isInConfig) feature.Disable();
            }
        }

        public static void Verify(Type type)
        {
            if (TryGetFeature(type, out var feature))
            {
                var isInConfig = Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name);
                var isDisabled = feature.IsDisabled;

                if (isDisabled && !isInConfig) feature.Enable();
                if (!isDisabled && isInConfig) feature.Disable();
            }
        }

        public static void Verify(IFeature feature)
        {
            var isInConfig = Plugin.Config.FeatureSettings.Disabled.Contains(feature.Name);
            var isDisabled = feature.IsDisabled;

            if (isDisabled && !isInConfig) feature.Enable();
            if (!isDisabled && isInConfig) feature.Disable();
        }

        public static void VerifyAll() => _features.ForEach(Verify);

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
            Verify(feature);
            if (feature.IsDisabled) return;
            feature.Load();
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
            Verify(feature);
            if (feature.IsDisabled) return;
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
        public static bool TryGetFeature(string name, out IFeature feature)
        {
            feature = _features.FirstOrDefault(x => x.Name == name);
            return feature != null;
        }

        public static TFeature GetFeature<TFeature>() where TFeature : IFeature => TryGetFeature<TFeature>(out var feature) ? feature : default;
        public static bool TryGetFeature<TFeature>(out TFeature feature) where TFeature : IFeature
        {
            feature = _features.FirstOrDefault(x => x is TFeature).As<TFeature>();
            return feature != null;
        }

        public static IFeature GetFeature(Type type) => TryGetFeature(type, out var feature) ? feature : null;
        public static bool TryGetFeature(Type type, out IFeature feature)
        {
            feature = _features.FirstOrDefault(x => x.GetType() == type);
            return feature != null;
        }

        public static void Load()
        {
            _features.ForEach(x =>
            {
                if (!x.IsRunning)
                {
                    if (x.IsDisabled) return;

                    x.Enable();
                    x.Load();
                }
            });
        }

        public static void Unload()
        {
            _features.ForEach(x =>
            {
                Verify(x);

                if (x.IsRunning)
                {
                    x.Disable();
                    x.Unload();
                }
            });
        }

        public static void Reload()
        {
            _features.ForEach(x =>
            {
                Verify(x);

                if (x.IsDisabled && x.IsRunning)
                {
                    x.Disable();
                    x.Unload();
                }
                else if (!x.IsDisabled && !x.IsRunning)
                {
                    x.Enable();
                    x.Load();
                }
            });
        }
    }
}