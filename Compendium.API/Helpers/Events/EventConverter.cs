using Compendium.Attributes;

using helpers.Events;

using PluginAPI.Enums;
using PluginAPI.Events;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Helpers.Events
{
    public static class EventConverter
    {
        private static readonly Dictionary<ServerEventType, EventProvider> m_Events = new Dictionary<ServerEventType, EventProvider>();

        public static IReadOnlyDictionary<ServerEventType, EventProvider> Events => m_Events;

        public static void Initialize()
        {
            foreach (var evType in Enum
                .GetValues(typeof(ServerEventType))
                .Cast<ServerEventType>())
            {
                m_Events[evType] = new EventProvider(evType.ToString());
            }

            PluginAPI.Events.EventManager.OnExecutingEvent += OnExecutingEvent;
        }

        private static void OnExecutingEvent(ServerEventType arg1, IEventArguments arg2, Event arg3, ValueContainer arg4, ValueContainer arg5)
        {
            if (m_Events.TryGetValue(arg1, out var provider))
            {
                provider.Invoke(arg2, arg4, arg5);
            }
        }

        public static EventProvider GetProvider(this ServerEventType serverEventType) 
            => Events[serverEventType];

        public static void AddHandler<THandler>(this ServerEventType serverEventType, THandler target) where THandler : Delegate
            => serverEventType.GetProvider()?.Register(target);

        public static void RemoveHandler<THandler>(this ServerEventType serverEventType, THandler target) where THandler : Delegate
            => serverEventType.GetProvider()?.Unregister(target);

        public static void RemoveAllHandlers(this ServerEventType serverEventType)
            => serverEventType.GetProvider()?.UnregisterAll();
    }
}
