using PluginAPI.Core.Interfaces;

using System;

namespace Compendium.Extensions
{
    public static class ReflectionExtensions
    {
        public static bool IsPlayerType(this Type type) => typeof(IPlayer).IsAssignableFrom(type);
    }
}
