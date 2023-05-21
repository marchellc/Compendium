using Compendium.Attributes;

using helpers.Events;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Helpers.Events
{
    public static class EventConverter
    {
        private static readonly Dictionary<ServerEventType, EventProvider> m_Events = new Dictionary<ServerEventType, EventProvider>();
        public static IReadOnlyDictionary<ServerEventType, EventProvider> Events => m_Events;

        [InitOnLoad(Priority = 254)]
        public static void Initialize()
        {
            foreach (var evType in Enum
                .GetValues(typeof(ServerEventType))
                .Cast<ServerEventType>())
            {
                m_Events[evType] = new EventProvider();
            }
        }

        public static EventProvider GetProvider(this ServerEventType serverEventType) => Events[serverEventType];
    }
}
