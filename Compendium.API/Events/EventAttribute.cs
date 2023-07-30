using PluginAPI.Enums;

using System;

namespace Compendium.Events
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EventAttribute : Attribute
    {
        public ServerEventType? Type { get; internal set; } = null;

        public EventAttribute(ServerEventType type)
            => Type = type;

        public EventAttribute() { }
    }
}