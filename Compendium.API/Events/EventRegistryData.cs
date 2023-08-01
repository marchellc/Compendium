﻿using helpers;

using PluginAPI.Events;

using System;
using System.Reflection;

namespace Compendium.Events
{
    public class EventRegistryData
    {
        public MethodInfo Method;
        public ParameterInfo[] Params;

        public Action<IEventArguments, ValueContainer, ValueContainer> Executor;

        public Priority Priority;

        public object Instance;
        public object[] EvBuffer;
    }
}