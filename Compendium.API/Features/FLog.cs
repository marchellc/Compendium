using helpers.Extensions;
using helpers.Json;

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System;

using PluginAPI.Core;

using Compendium.Logging;

namespace Compendium.Features
{
    public static class FLog
    {
        public static void Warn(object message, params DebugParameter[] parameters)
        {
            var stack = new StackTrace();
            var frame = stack.GetFrames().Skip(1).First();
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            var assembly = type.Assembly;
            var msg = message.ToString();

            if (!TryGetLogName(assembly, out var logName))
                throw new InvalidOperationException($"Failed to find log name for type: {type.FullName}");

            if (parameters != null && parameters.Any())
                msg += $"\nParameters ({parameters.Length}):\n{JsonHelper.ToJson(parameters)}";

            Log.Warning(msg, logName);
        }

        public static void Debug(object message, params DebugParameter[] parameters)
        {
            var stack = new StackTrace();
            var frame = stack.GetFrames().Skip(1).First();
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            var assembly = type.Assembly;
            var msg = message.ToString();

            if (!assembly.CanDebug(out var feature))
                return;

            if (parameters != null && parameters.Any())
                msg += $"\nParameters ({parameters.Length}):\n{JsonHelper.ToJson(parameters)}";

            Log.Debug(msg, true, feature.Name);
        }

        public static void Error(object message, params DebugParameter[] parameters)
        {
            var stack = new StackTrace();
            var frame = stack.GetFrames().Skip(1).First();
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            var assembly = type.Assembly;
            var msg = message.ToString();

            if (!TryGetLogName(assembly, out var logName))
                throw new InvalidOperationException($"Failed to find log name for type: {type.FullName}");

            if (parameters != null && parameters.Any())
                msg += $"\nParameters ({parameters.Length}):\n{JsonHelper.ToJson(parameters)}";

            Log.Error(msg, logName);
        }

        public static void Info(object message, params DebugParameter[] parameters)
        {
            var stack = new StackTrace();
            var frame = stack.GetFrames().Skip(1).First();
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            var assembly = type.Assembly;
            var msg = message.ToString();

            if (!TryGetLogName(assembly, out var logName))
                throw new InvalidOperationException($"Failed to find log name for type: {type.FullName}");

            if (parameters != null && parameters.Any())
                msg += $"\nParameters ({parameters.Length}):\n{JsonHelper.ToJson(parameters)}";

            Log.Info(msg, logName);
        }

        public static bool CanDebug(this Assembly assembly, out IFeature feature)
        {
            if (FeatureManager.LoadedFeatures.TryGetFirst(f => f.GetType().Assembly == assembly, out feature))
                return Plugin.Config.FeatureSettings.Debug.Contains(feature.Name) || Plugin.Config.FeatureSettings.Debug.Contains("*");

            return false;
        }

        private static bool TryGetLogName(Assembly assembly, out string name)
        {
            if (FeatureManager.LoadedFeatures.TryGetFirst(f => f.GetType().Assembly == assembly, out var feature))
            {
                name = feature.Name ?? feature.GetType().Name;
                return true;
            }

            name = null;
            return false;
        }
    }
}