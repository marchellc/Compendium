using helpers;
using helpers.Dynamic;
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
                else if (del is Func<bool> func)
                {
                    result = func();
                    return;
                }
                else if (del is DynamicMethodDelegate methodDelegate)
                {
                    data.PrepareBuffer(args, isAllowed);

                    if (Plugin.Config.ApiSetttings.EventSettings.UseStable)
                    {
                        var res = data.Target.Method.Invoke(data.Handle, data.Buffer);

                        if (res != null && res is bool b)
                            result = b;
                        else
                            result = true;

                        return;
                    }
                    else
                    {
                        var res = methodDelegate(data.Handle, data.Buffer);

                        if (res != null && res is bool b)
                            result = b;
                        else
                            result = true;

                        return;
                    }
                }
                else
                    Plugin.Warn($"Failed to invoke delegate '{del.GetType().FullName}' ({data.Target.Method.ToLogName()}) - unknown delegate type");
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to invoke delegate '{data.Target.Method.ToLogName()}' while executing event '{args.BaseType}'");
                Plugin.Error(ex);
            }

            result = true;
        }

        public static bool TryCreateEventData(MethodInfo target, bool skipAttributeCheck, object handle, out EventRegistryData registryData)
        {
            if (target is null)
            {
                registryData = null;
                return false;
            }

            EventAttribute evAttr = null;

            if (!skipAttributeCheck)
            {
                if (!target.TryGetAttribute(out evAttr))
                {
                    registryData = null;
                    return false;
                }
            }

            if (!TryValidateInstance(target, ref handle))
            {
                registryData = null;
                return false;
            }

            var parameters = target.GetParameters();
            var evType = (evAttr?.Type.HasValue ?? false) ? evAttr.Type.Value : ServerEventType.None;

            if (evType is ServerEventType.None)
            {
                if (!TryRecognizeEventType(target, parameters, out evType))
                {
                    registryData = null;
                    return false;
                }
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

            registryData = new EventRegistryData(del, evAttr?.Priority ?? Priority.Normal, evType, handle, buffer, typeParams);
            return true;
        }

        public static bool TryGenerateDelegate(MethodInfo method, object handle, out Delegate del)
        {
            try
            {
                var parameters = method.GetParameters();

                if (parameters.Length <= 0)
                {
                    if (method.ReturnType == typeof(void))
                    {
                        del = method.CreateDelegate(typeof(Action), handle);
                        return true;
                    }
                    else if (method.ReturnType == typeof(bool))
                    {
                        del = method.CreateDelegate(typeof(Func<bool>), handle);
                        return true;
                    }
                    else
                    {
                        Plugin.Warn($"Cannot create invocation delegate for event handler '{method.ToLogName()}': unsupported return type ({method.ReturnType.FullName})");

                        del = null;
                        return false;
                    }
                }

                if (!Reflection.HasInterface<IEventArguments>(parameters[0].ParameterType))
                {
                    Plugin.Warn($"Event handler '{method.ToLogName()}' has invalid event argument at index '0' (expected a class deriving from IEventArguments, actual class is '{parameters[0].ParameterType.FullName}')");

                    del = null;
                    return false;
                }

                if (parameters.Length == 2)
                {
                    if (parameters[1].ParameterType != typeof(ValueReference))
                    {
                        Plugin.Warn($"Event handler '{method.ToLogName()}' has invalid event argument at index '1' (expected a ValueReference, actual class is '{parameters[1].ParameterType.FullName}')");

                        del = null;
                        return false;
                    }
                }

                if (parameters.Length > 2)
                {
                    Plugin.Warn($"Event handler '{method.ToLogName()}' has too many arguments!");

                    del = null;
                    return false;
                }

                del = method.GetOrCreateInvoker();
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

        public static bool TryRecognizeEventType(MethodInfo method, ParameterInfo[] parameters, out ServerEventType serverEventType)
        {
            if (!parameters.Any())
            {
                Plugin.Warn($"Failed to recognize event type of event handler '{method.ToLogName()}': no recognizable event parameters");

                serverEventType = default;
                return false;
            }

            Type evParameterType = null;

            foreach (var p in parameters)
            {
                if (Reflection.HasInterface<IEventArguments>(p.ParameterType))
                {
                    evParameterType = p.ParameterType;
                    break;
                }
            }

            if (evParameterType is null)
            {
                Plugin.Warn($"Failed to recognize event type of event handler '{method.ToLogName()}': no recognizable event parameters");

                serverEventType = default;
                return false;
            }

            if (!EventManager.Events.TryGetFirst(ev => ev.Value.EventArgType == evParameterType, out var infoPair)
                || infoPair.Value is null)
            {
                Plugin.Warn($"Failed to recognize event type of event handler '{method.ToLogName()}': unknown event type");

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

                Plugin.Warn($"Failed to register event handler '{method.ToLogName()}': missing class instance");
                return false;
            }

            return true;
        }
    }
}