using helpers;

using PluginAPI.Events;

using System;
using System.Reflection;

namespace Compendium.Events
{
    public class EventRegistryData
    {
        public MethodInfo Method;
        public ParameterInfo[] Params;

        public Action<IEventArguments, ValueReference, ValueReference> Executor;

        public Priority Priority;

        public object Instance;
        public object[] EvBuffer;
    }
}