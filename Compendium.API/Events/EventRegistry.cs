using Compendium.Round;

using helpers;
using helpers.Attributes;
using helpers.Extensions;

using PluginAPI.Enums;
using PluginAPI.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Compendium.Events
{
    public static class EventRegistry
    {
        private static readonly Dictionary<ServerEventType, HashSet<EventRegistryData>> _registry = new Dictionary<ServerEventType, HashSet<EventRegistryData>>();
        private static readonly Dictionary<UpdateType, HashSet<Action>> _updateRegistry = new Dictionary<UpdateType, HashSet<Action>>();

        [Load]
        private static void Initialize()
        {
            EventBridge.OnExecuting = OnExecutingEvent;

            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", OnUpdate);
            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", OnFixedUpdate);
            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnLateUpdate", OnLateUpdate);
        }

        [Unload]
        private static void Unload()
        {
            EventBridge.OnExecuting = null;

            Reflection.TryRemoveHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", OnUpdate);
            Reflection.TryRemoveHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", OnFixedUpdate);
            Reflection.TryRemoveHandler<Action>(typeof(StaticUnityMethods), "OnLateUpdate", OnLateUpdate);

            _registry.Clear();
            _updateRegistry.Clear();
        }

        public static void RegisterEvents(object instance)
        {
            if (instance is null)
                return;

            RegisterEvents(instance.GetType(), instance);
        }

        public static void RegisterEvents(Assembly assembly)
            => assembly.ForEachType(t => RegisterEvents(t));

        public static void RegisterEvents(Type type, object instance = null)
            => type.ForEachMethod(m => RegisterEvents(m, instance));

        public static void RegisterEvents(MethodInfo method, object instance = null)
        {
            if (method.DeclaringType.Namespace.StartsWith("System"))
                return;

            if (method.TryGetAttribute<EventAttribute>(out var eventAttribute))
            {
                if (IsRegistered(method, instance))
                {
                    Plugin.Warn($"Attempted to register a duplicate event handler (method: {method.ToLogName(false)} | instance: {instance?.ToString() ?? "not provided"})");
                    return;
                }

                if (!TryValidateInstance(method, ref instance))
                    return;

                var parameters = method.GetParameters();

                if (!eventAttribute.Type.HasValue)
                {
                    if (!TryRecognizeEventType(parameters, out var evType))
                    {
                        Plugin.Warn($"Failed to automatically recognize event type of method: {method.ToLogName(false)}");
                        return;
                    }

                    eventAttribute.Type = evType;
                }

                var data = new EventRegistryData();

                data.Instance = instance;
                data.Method = method;
                data.Params = parameters;
                data.EvBuffer = parameters != null && parameters.Any() ? new object[parameters.Length] : null;

                data.Executor = (args, isAllowed, shouldContinue) =>
                {
                    object[] objArray = data.EvBuffer;

                    if (objArray != null)
                    {
                        for (int i = 0; i < data.Params.Length; i++)
                        {
                            if (Reflection.HasInterface<IEventArguments>(data.Params[i].ParameterType))
                            {
                                objArray[i] = args;
                                continue;
                            }

                            if (data.Params[i].ParameterType == typeof(ValueReference))
                            {
                                if (data.Params[i].Name == "isAllowed")
                                {
                                    objArray[i] = isAllowed;
                                    continue;
                                }

                                if (data.Params[i].Name == "shouldContinue")
                                {
                                    objArray[i] = shouldContinue;
                                    continue;
                                }

                                Plugin.Warn($"Unrecognized ValueReference argument ({data.Params[i].Name}) at index {i} in method {data.Method.ToLogName(false)}!");
                                continue;
                            }

                            objArray[i] = default;
                            Plugin.Warn($"Unknown argument in method {data.Method.ToLogName(false)} at index {i} (name: {data.Params[i].Name}), supplying default value.");
                        }
                    }

                    try
                    {
                        var result = data.Method.Invoke(data.Instance, objArray);

                        if (result != null)
                        {
                            if (result is bool isAllowedB)
                            {
                                isAllowed.Value = isAllowedB;
                                return;
                            }
                            else if (result is Tuple<bool, bool> tuple)
                            {
                                isAllowed.Value = tuple.Item1;
                                shouldContinue.Value = tuple.Item2;

                                return;
                            }
                            else if (result is IEventCancellation cancellation)
                            {
                                isAllowed.Value = cancellation;
                                return;
                            }
                            else if (result is Tuple<IEventCancellation, bool> tup)
                            {
                                isAllowed.Value = tup.Item1;
                                shouldContinue.Value = tup.Item2;

                                return;
                            }

                            Plugin.Warn($"Unknown return type ({result.GetType().FullName}) in method {data.Method.ToLogName(false)}!");
                        }

                        if (data.EvBuffer != null)
                            Array.Clear(data.EvBuffer, 0, data.EvBuffer.Length);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Error($"Failed to execute event {args.BaseType} ({data.Method.ToLogName(false)}):");
                        Plugin.Error(ex);
                    }
                };

                if (_registry.TryGetValue(eventAttribute.Type.Value, out var list))
                    list.Add(data);
                else
                    _registry.Add(eventAttribute.Type.Value, new HashSet<EventRegistryData>() { data });
            }
            else
            {
                if (!TryValidateInstance(method, ref instance))
                    return;

                if (IsRegistered(method, instance, false))
                    return;

                if (method.TryGetAttribute<FixedUpdateEventAttribute>(out _))
                {
                    var del = (Action)(method.IsStatic ? method.CreateDelegate(typeof(Action)) : method.CreateDelegate(typeof(Action), instance));

                    if (del != null)
                    {
                        if (_updateRegistry.ContainsKey(UpdateType.Fixed))
                            _updateRegistry[UpdateType.Fixed].Add(del);
                        else
                            _updateRegistry[UpdateType.Fixed] = new HashSet<Action>() { del };
                    }
                }
                else if (method.TryGetAttribute<LateUpdateEventAttribute>(out _))
                {
                    var del = (Action)(method.IsStatic ? method.CreateDelegate(typeof(Action)) : method.CreateDelegate(typeof(Action), instance));

                    if (del != null)
                    {
                        if (_updateRegistry.ContainsKey(UpdateType.Late))
                            _updateRegistry[UpdateType.Late].Add(del);
                        else
                            _updateRegistry[UpdateType.Late] = new HashSet<Action>() { del };
                    }
                }
                else if (method.TryGetAttribute<UpdateEventAttribute>(out _))
                {
                    var del = (Action)(method.IsStatic ? method.CreateDelegate(typeof(Action)) : method.CreateDelegate(typeof(Action), instance));

                    if (del != null)
                    {
                        if (_updateRegistry.ContainsKey(UpdateType.Normal))
                            _updateRegistry[UpdateType.Normal].Add(del);
                        else
                            _updateRegistry[UpdateType.Normal] = new HashSet<Action>() { del };
                    }
                }
            }
        }

        public static bool IsRegistered(MethodInfo method, object instance = null, bool eventMode = true)
        {
            if (!TryValidateInstance(method, ref instance))
                return false;

            return eventMode ? _registry.TryGetFirst(list =>
            {
                return list.Value.TryGetFirst(ev =>
                {
                    if (instance != null && ev.Instance is null)
                        return false;

                    if (instance is null && ev.Instance != null)
                        return false;

                    if (instance != ev.Instance)
                        return false;

                    if (method != ev.Method)
                        return false;

                    return true;
                }, out _);
            }, out _) : _updateRegistry.Values.Any(val =>
            {
                return val.Any(v =>
                {
                    if (instance != null && v.Target is null)
                        return false;

                    if (instance is null && v.Target != null)
                        return false;

                    if (instance != v.Target)
                        return false;

                    if (method != v.Method)
                        return false;

                    return true;
                });
            });
        }

        public static void UnregisterEvents(object instance)
        {
            if (instance is null)
                return;

            UnregisterEvents(instance.GetType(), instance);
        }

        public static void UnregisterEvents(Assembly assembly)
            => assembly.ForEachType(t => UnregisterEvents(t));

        public static void UnregisterEvents(Type type, object instance = null)
            => type.ForEachMethod(m => UnregisterEvents(m, instance));

        public static void UnregisterEvents(MethodInfo method, object instance = null)
        {
            if (!TryValidateInstance(method, ref instance))
                return;

            var count = 0;
            var uCount = 0;

            _registry.Values.ForEach(list =>
            {
                count += list.RemoveWhere(ev =>
                {
                    if (instance != null && ev.Instance is null)
                        return false;

                    if (instance is null && ev.Instance != null)
                        return false;

                    if (instance != ev.Instance)
                        return false;

                    if (method != ev.Method)
                        return false;

                    return true;
                });
            });

            _updateRegistry.Values.ForEach(list =>
            {
                uCount += list.RemoveWhere(v =>
                {
                    if (instance != null && v.Target is null)
                        return false;

                    if (instance is null && v.Target != null)
                        return false;

                    if (instance != v.Target)
                        return false;

                    if (method != v.Method)
                        return false;

                    return true;
                });
            });
        }

        private static bool TryValidateInstance(MethodInfo method, ref object instance)
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

        private static bool TryRecognizeEventType(ParameterInfo[] parameters, out ServerEventType serverEventType)
        {
            if (!parameters.Any())
            {
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
                serverEventType = default;
                return false;
            }

            if (!EventManager.Events.TryGetFirst(ev => ev.Value.EventArgType == evParameterType, out var infoPair) || infoPair.Value is null)
            {
                serverEventType = default;
                return false;
            }

            serverEventType = infoPair.Key;
            return true;
        }

        private static bool OnExecutingEvent(IEventArguments args, Event ev, ValueReference isAllowed)
        {
            var continueExec = new ValueReference() { Value = true };

            if (_registry.TryGetValue(args.BaseType, out var list))
            {
                foreach (var evData in list)
                {
                    if (continueExec.Value != null && continueExec.Value is bool shC && !shC)
                        break;

                    Calls.Delegate(evData.Executor, args, isAllowed, continueExec);
                }
            }

            if (continueExec.Value != null && continueExec.Value is bool shContinue)
                return shContinue;

            return true;
        }

        private static void FireUpdate(UpdateType type)
        {
            if (!StaticUnityMethods.IsPlaying || !RoundHelper.IsReady)
                return;

            if (_updateRegistry.TryGetValue(type, out var list))
            {
                foreach (var handler in list)
                {
                    Calls.Delegate(handler);
                }
            }
        }

        private static void OnUpdate()
            => FireUpdate(UpdateType.Normal);

        private static void OnLateUpdate()
            => FireUpdate(UpdateType.Late);

        private static void OnFixedUpdate()
            => FireUpdate(UpdateType.Fixed);
    }
}
