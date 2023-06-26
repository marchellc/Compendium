using PluginAPI.Core;

namespace Compendium.Features
{
    public static class FeatureExtensions
    {
        public static void Info(this IFeature feature, object message) => Log.Info(message.ToString(), feature.Name);
        public static void Debug(this IFeature feature, object message) => Log.Debug(message.ToString(), Plugin.Config.LogSettings.ShowDebug, feature.Name);
        public static void Error(this IFeature feature, object message) => Log.Error(message.ToString(), feature.Name);
        public static void Warn(this IFeature feature, object message) => Log.Warning(message.ToString(), feature.Name);
    }
}