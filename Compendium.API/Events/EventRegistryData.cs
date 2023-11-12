using Compendium.Value;
using helpers;

using PluginAPI.Enums;
using PluginAPI.Events;

using System;

namespace Compendium.Events
{
    public class EventRegistryData
    {
        public Delegate Target { get; set; }

        public Priority Priority { get; set; }
        public ServerEventType Type { get; set; }

        public EventStatistics Stats { get; }

        public object Handle { get; }
        public object[] Buffer { get; }

        public Type[] Args { get; }

        public EventRegistryData(Delegate target, Priority priority, ServerEventType type, object handle, object[] buffer, Type[] args)
        {
            Target = target;
            Priority = priority;
            Type = type;
            Handle = handle;
            Buffer = buffer;
            Args = args;

            Stats = new EventStatistics();
        }

        public void PrepareBuffer(IEventArguments args, ValueReference isAllowed)
        {
            if (Buffer is null)
                return;

            if (Args.Length == 1)
                Buffer[0] = args;

            if (Args.Length == 2)
            {
                Buffer[0] = args;
                Buffer[1] = isAllowed;
            }
        }
    }
}