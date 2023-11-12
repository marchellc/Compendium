using BetterCommands;
using BetterCommands.Management;

using Compendium.Constants;
using Compendium.Events;
using Compendium;

using GameCore;

using helpers;
using helpers.Attributes;
using helpers.Extensions;
using helpers.Patching;
using helpers.Pooling.Pools;
using helpers.Time;

using PluginAPI.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Updating;

namespace Compendium.Features
{
    public static class FeatureManager
    {
        private static readonly List<Type> _knownFeatures = new List<Type>();
        private static readonly List<IFeature> _features = new List<IFeature>();

        public static IReadOnlyList<IFeature> LoadedFeatures => _features;
        public static IReadOnlyList<Type> RegisteredFeatures => _knownFeatures;

        [Load]
        public static void Reload()
        {
            Unload();

            foreach (var file in Directory.GetFiles(Directories.ThisFeatures, "*.dll"))
            {
                try
                {
                    var rawAssembly = File.ReadAllBytes(file);
                    var assembly = Assembly.Load(rawAssembly);

                    foreach (var type in assembly.GetTypes())
                    {
                        if (!Reflection.HasInterface<IFeature>(type))
                            continue;

                        if (_knownFeatures.Contains(type))
                        {
                            Plugin.Warn($"Feature '{type.FullName}' is already loaded.");
                            continue;
                        }

                        if (_features.Any(f => f.GetType() == type))
                        {
                            Plugin.Warn($"Feature '{type.FullName}' is already enabled.");
                            continue;
                        }

                        _knownFeatures.Add(type);
                        _features.Add(Reflection.Instantiate<IFeature>(type));
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed to load file: {file}");
                    Plugin.Error(ex);
                }
            }

            Plugin.Info($"Found {_features.Count} features!");

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
                    Singleton.Set(feature);

                    var assembly = feature.GetType().Assembly;

                    feature.Load();

                    AttributeLoader.ExecuteLoadAttributes(assembly);

                    if (feature.IsPatch)
                        PatchManager.PatchAssemblies(assembly);

                    EventRegistry.RegisterEvents(assembly);
                    CommandManager.Register(assembly);

                    UpdateHandler.Register(assembly);

                    AttributeRegistry<RoundStateChangedAttribute>.Register(assembly);

                    Plugin.Info($"Loaded feature '{feature.Name}'");
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
                var assembly = feature.GetType().Assembly;

                AttributeRegistry<RoundStateChangedAttribute>.Unregister(assembly);

                UpdateHandler.Unregister(assembly);

                AttributeLoader.ExecuteUnloadAttributes(assembly);

                if (feature.IsPatch)
                    PatchManager.UnpatchAssemblies(assembly);

                EventRegistry.UnregisterEvents(assembly);

                var rList = ListPool<CommandData>.Pool.Get();

                CommandManager.Commands.ForEach(cmd =>
                {
                    cmd.Value.ForEach(c =>
                    {
                        if (c.DeclaringType.Assembly == assembly)
                            rList.Add(c);
                    });
                });

                rList.ForEach(cmd => CommandManager.TryUnregister(cmd.Name, CommandType.RemoteAdmin));
                rList.ForEach(cmd => CommandManager.TryUnregister(cmd.Name, CommandType.GameConsole));
                rList.ForEach(cmd => CommandManager.TryUnregister(cmd.Name, CommandType.PlayerConsole));

                rList.ReturnList();

                feature.Unload();

                _features.Remove(feature);
                _knownFeatures.Remove(feature.GetType());

                Singleton.Dispose(feature.GetType());

                Plugin.Info($"Unloaded feature '{feature.Name}'");
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to unload feature {feature.Name}:\n{ex}");
            }
        }

        public static void Register<TFeature>() where TFeature : IFeature 
            => Register(typeof(TFeature));

        public static void Register(Type type)
        {
            if (!_knownFeatures.Contains(type))
            {
                _knownFeatures.Add(type);
                _features.Add(Reflection.Instantiate<IFeature>(type));
            }
        }

        public static void Unregister<TFeature>() where TFeature : IFeature 
            => Unregister(typeof(TFeature));

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

        public static IFeature GetFeature(string name) 
            => TryGetFeature(name, out var feature) ? feature : null;

        public static TFeature GetFeature<TFeature>() where TFeature : IFeature 
            => TryGetFeature<TFeature>(out var feature) ? feature : default;

        public static IFeature GetFeature(Type type) 
            => TryGetFeature(type, out var feature) ? feature : null;

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
        }

        public static void Unload()
        {
            _features.ForEach(Unload);
            _features.Clear();
            _knownFeatures.Clear();
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnWaiting()
        {
            if (Plugin.Config.ApiSetttings.ReloadOnRestart)
            {
                Plugin.LoadConfig();
                ConfigFile.ReloadGameConfigs();
            }

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
        }

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRestart()
        {
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

        [Update]
        private static void OnUpdate()
        {
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

        [Command("dfeature", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("df")]
        [Description("Disables the specified feature.")]
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

        [Command("efeature", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("ef")]
        [Description("Enables the specified feature.")]
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

        [Command("rfeature", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("rf")]
        [Description("Reloads the specified feature.")]
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

        [Command("lfeatures", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("lf")]
        [Description("Lists all available features.")]
        private static string ListFeatures(Player sender)
        {
            if (!_features.Any())
                return $"There aren't any loaded features ({_features.Count}/{_knownFeatures.Count})";

            var sb = StringBuilderPool.Pool.Get();

            sb.AppendLine($"Showing a list of {_features.Count} features:");

            _features.For((i, feature) =>
            {
                var assembly = feature.GetType().Assembly;
                sb.AppendLine($"<b>[{i + 1}] </b> {Colors.LightGreen(feature?.Name ?? "UNKNOWN NAME")} v{assembly.GetName().Version} [{(feature.IsEnabled ? Colors.Green("ENABLED") : Colors.Red("DISABLED"))}]{(feature.IsPatch ? " <i>(contains patches)</i>" : "")}");
            });

            return StringBuilderPool.Pool.PushReturn(sb);
        }

        [Command("dtfeature", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("dtf")]
        [Description("Shows all details about a feature.")]
        private static string DetailFeature(Player sender, string featureName)
        {
            if (!_features.TryGetFirst(f => f.Name.GetSimilarity(featureName) >= 0.8, out var feature))
                return "There are no features matching your query.";

            if (feature is null)
                return "The requested feature's instance is null.";

            var type = feature.GetType();
            var assembly = type.Assembly;
            var assemblyName = assembly.GetName();
            var filePath = $"{Directories.ThisFeatures}/{assemblyName.Name}.dll";
            var fileExists = File.Exists(filePath);
            var fileDate = fileExists ? File.GetLastWriteTime(filePath).ToLocalTime() : DateTime.MinValue;
            var fileDateStr = $"{fileDate.ToString("F")} ({TimeUtils.UserFriendlySpan((TimeUtils.LocalTime - fileDate))} ago)";
            var sb = StringBuilderPool.Pool.Get();

            sb.AppendLine("== Feature Detail ==");
            sb.AppendLine($"- Name: {feature.Name}");
            sb.AppendLine($"- Main class: {type.FullName}");

            sb.AppendLine($"- Assembly: {assemblyName.FullName}");
            sb.AppendLine($"- Version: {assemblyName.Version}");

            sb.AppendLine($"- File Location: {filePath}");
            sb.AppendLine($"- File Time: {fileDateStr}");

            if (feature is ConfigFeatureBase configFeature)
                sb.AppendLine($"- Config File Location: {configFeature.Config?.Path ?? "UNKNOWN"}");

            if (feature.IsPatch)
                sb.AppendLine($"- Contains Patches");

            sb.AppendLine();
            sb.AppendLine($"Listing all types ..");

            var types = assembly.GetTypes().OrderBy(t => t.FullName);

            types.For((i, t) =>
            {
                sb.AppendLine($"[ {i} ]: {t.FullName} ({(t.IsSealed && t.IsAbstract ? "static" : "instance")})");
            });

            return StringBuilderPool.Pool.PushReturn(sb);
        }
    }
}