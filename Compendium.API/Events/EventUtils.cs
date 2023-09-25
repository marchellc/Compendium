using Compendium.Reflect.Dynamic;

using helpers;
using helpers.Extensions;

using PluginAPI.Enums;
using PluginAPI.Events;

using System;
using System.Linq;
using System.Reflection;

namespace Compendium.Events
{
    public static class EventUtils
    {
        public static void TryInvoke(EventRegistryData data, IEventArguments args, ValueReference isAllowed, out bool result)
        {
            var del = data.Target;

            try
            {
                if (del is Action action)
                {
                    action();
                    result = true;
                    return;
                }

                if (del is DynamicMethodDelegate methodDelegate)
                {
                    data.PrepareBuffer(args, isAllowed);

                    var res = methodDelegate(del.Target, data.Buffer);

                    if (res != null && res is bool b)
                        result = b;
                    else
                        result = true;

                    return;
                }

                Plugin.Error($"Failed to invoke delegate '{del.GetType().FullName}' ({DynamicMethodDelegateFactory.GetMethodName(data.Target.Method)}) - unknown delegate type");
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to invoke delegate '{DynamicMethodDelegateFactory.GetMethodName(data.Target.Method)}' while executing event '{args.BaseType}'");
                Plugin.Error(ex);
            }

            result = true;
        }

        public static bool TryCreateEventData(MethodInfo target, object handle, out EventRegistryData registryData)
        {
            if (target is null)
            {
                registryData = null;
                return false;
            }

            if (!target.TryGetAttribute<EventAttribute>(out var evAttr))
            {
                registryData = null;
                return false;
            }

            if (!TryValidateInstance(target, ref handle))
            {
                registryData = null;
                return false;
            }

            var parameters = target.GetParameters();

            if (!evAttr.Type.HasValue)
            {
                if (!TryRecognizeEventType(parameters, out var evType))
                {
                    Plugin.Error($"Failed to automatically determine the event type of '{target.ToLogName()}'!");

                    registryData = null;
                    return false;
                }

                evAttr.Type = evType;
            }

            if (!TryGenerateDelegate(target, handle, out var del))
            {
                Plugin.Error($"Failed to generate calling delegate for method '{target.ToLogName()}'");

                registryData = null;
                return false;
            }

            object[] buffer = null;

            if (parameters.Length > 0)
                buffer = new object[parameters.Length];

            var typeParams = parameters.Select(x => x.ParameterType).ToArray();

            registryData = new EventRegistryData(del, evAttr.Priority, evAttr.Type.Value, handle, buffer, typeParams);
            return true;
        }

        public static bool TryGenerateDelegate(MethodInfo method, object handle, out Delegate del)
        {
            try
            {
                var parameters = method.GetParameters();

                if (parameters.Length <= 0)
                {
                    del = method.CreateDelegate(typeof(Action), handle);
                    return true;
                }

                del = DynamicMethodDelegateFactory.Create(method);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to generate delegate for {method.ToLogName()}");
                Plugin.Error(ex);

                del = null;
                return false;
            }
        }

        public static bool TryRecognizeEventType(ParameterInfo[] parameters, out ServerEventType serverEventType)
        {
            if (!parameters.Any())
            {
                serverEventType = default;
                return false;
            }

            Type evParameterType = null;

            foreach (var p in parameters)
            {
                if (helpers.Reflection.HasInterface<IEventArguments>(p.ParameterType))
                {
                    evParameterType = p.ParameterType;
                    break;
                }
            }

            if (evParameterType is null)
            {
                serverEventType = default;
                return false;
            }

            if (!EventManager.Events.TryGetFirst(ev => ev.Value.EventArgType == evParameterType, out var infoPair)
                || infoPair.Value is null)
            {
                serverEventType = default;
                return false;
            }

            serverEventType = infoPair.Key;
            return true;
        }

        public static bool TryValidateInstance(MethodInfo method, ref object instance)
        {
            if (instance is null && !method.IsStatic)
            {
                if (Singleton.HasInstance(method.DeclaringType))
                {
                    instance = Singleton.Instance(method.DeclaringType);
                    return true;
                }

                return false;
            }

            return true;
        }
    }
}