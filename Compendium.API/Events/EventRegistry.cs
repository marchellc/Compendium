using BetterCommands;

using Compendium.Attributes;
using Compendium.Comparison;
using Compendium.Enums;
using Compendium.Value;
using helpers;
using helpers.Attributes;
using helpers.Dynamic;
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
        private static bool _everExecuted;
        private static List<EventRegistryData> _registry = new List<EventRegistryData>();

        public static List<ServerEventType> RecordEvents => Plugin.Config.ApiSetttings.EventSettings.RecordEvents;

        public static bool RoundSummary => Plugin.Config.ApiSetttings.EventSettings.ShowRoundSummary || DebugOverride;
        public static bool LogExecutionTime => Plugin.Config.ApiSetttings.EventSettings.ShowTotalExecution || DebugOverride;
        public static bool LogHandlers => Plugin.Config.ApiSetttings.EventSettings.ShowEventDuration || DebugOverride;

        public static bool DebugOverride;

        public static double HighestEventDuration => _registry.Where(x => x.Stats.LongestTime != -1).OrderByDescending(x => x.Stats.LongestTime).FirstOrDefault()?.Stats?.LongestTime ?? 0;
        public static double ShortestEventDuration => _registry.Where(x => x.Stats.ShortestTime != -1).OrderByDescending(x => x.Stats.ShortestTime).LastOrDefault()?.Stats?.ShortestTime ?? 0;
        public static double HighestTicksPerSecond => _registry.Where(x => x.Stats.TicksWhenLongest != 0).OrderByDescending(x => x.Stats.TicksWhenLongest).FirstOrDefault()?.Stats?.TicksWhenLongest ?? 0;

        [Load]
        private static void Initialize()
        {
            EventManager.Proxy = Proxy;
            Plugin.Debug($"Initialized event proxy.");
        }

        [Unload]
        private static void Unload()
        {
            EventManager.Proxy = null;

            _registry.Clear();
        }

        private static object Proxy(object arg1, Type type, Event @event, IEventArguments arguments)
        {
            try
            {
                if (!_registry.Any(ev => ev.Type == arguments.BaseType))
                    return true;

                _everExecuted = true;

                var startTime = DateTime.Now;
                var list = _registry.Where(x => x.Type == arguments.BaseType);
                var result = true;
                var isAllowed = new ValueReference(arg1, type);

                foreach (var ev in list)
                {
                    var startEv = DateTime.Now;

                    EventUtils.TryInvoke(ev, arguments, isAllowed, out result);

                    if (RecordEvents.Contains(ev.Type))
                    {
                        var endEv = DateTime.Now;
                        var durationEv = TimeSpan.FromTicks((endEv - startEv).Ticks);

                        ev.Stats.Record(durationEv.TotalMilliseconds);

                        if (LogHandlers)
                            Plugin.Debug($"Finished executing '{ev.Type}' handler '{ev.Target.Method.ToLogName()}' in {durationEv.TotalMilliseconds} ms");
                    }
                }

                if (isAllowed.Value is null)
                    isAllowed.Value = result;

                if (!LogExecutionTime)
                    return result;

                var endTime = DateTime.Now;
                var duration = TimeSpan.FromTicks((endTime - startTime).Ticks);

                Plugin.Debug($"Total Event Execution of {arguments.BaseType} took {duration.TotalMilliseconds} ms.");

                if (arg1 != null && isAllowed.Value != null && arg1.GetType() == isAllowed.Value.GetType())
                    return isAllowed.Value;

                return arg1;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Caught an exception while executing event '{arguments.BaseType}'");
                Plugin.Error(ex);
            }

            return arg1;
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
            => type.ForEachMethod(m => RegisterEvents(m, false, instance));

        public static void RegisterEvents(MethodInfo method, bool skipAttributeCheck, object instance = null)
        {
            try
            {
                if (method.DeclaringType.Namespace.StartsWith("System"))
                    return;

                if (method.IsDefined(typeof(EventAttribute), false)
                    && !IsRegistered(method, instance)
                    && EventUtils.TryCreateEventData(method, skipAttributeCheck, instance, out var data))
                {
                    _registry.Add(data);
                    Plugin.Info($"Registered event '{data.Type}' ({data.Target.Method.ToLogName()})");
                }
            }
            catch (Exception ex)
            {
                Plugin.Error($"An error occured while registering event '{method.ToLogName()}'");
                Plugin.Error(ex);
            }
        }

        public static bool IsRegistered(MethodInfo method, object instance = null)
        {
            if (!EventUtils.TryValidateInstance(method, ref instance))
                return false;

            method = DynamicMethodCache.GetOriginalMethod(method);
            return _registry.TryGetFirst(ev => ev.Target.Method == method 
                        && NullableObjectComparison.Compare(ev.Target.Target, instance), out _);
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

        public static bool UnregisterEvents(MethodInfo method, object instance = null)
        {
            if (!EventUtils.TryValidateInstance(method, ref instance))
                return false;

            method = DynamicMethodCache.GetOriginalMethod(method);
            return _registry.RemoveAll(ev => ev.Target.Method == method
                        && NullableObjectComparison.Compare(ev.Target.Target, instance)) > 0;
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnWaiting()
        {
            if (_everExecuted)
            {
                var sb = Pools.PoolStringBuilder();
                var dict = Pools.PoolDictionary<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>>();

                _registry.ForEach(ev =>
                {
                    if (ev.Stats is null
                    || ev.Stats.LongestTime == -1
                    || ev.Stats.ShortestTime == -1
                    || ev.Stats.AverageTime == -1
                    || ev.Stats.LastTime == -1
                    || ev.Stats.TicksWhenLongest <= 0
                    || ev.Stats.Executions <= 0)
                        return;

                    if (!dict.ContainsKey(ev.Type))
                        dict[ev.Type] = Pools.PoolList<Tuple<string, double, double, double, double, double, int>>();

                    dict[ev.Type].Add(new Tuple<string, double, double, double, double, double, int>(
                        ev.Target.Method.ToLogName(),
                        ev.Stats.LongestTime,
                        ev.Stats.ShortestTime,
                        ev.Stats.AverageTime,
                        ev.Stats.LastTime,
                        ev.Stats.TicksWhenLongest,
                        ev.Stats.Executions));
                });

                sb.AppendLine();

                dict.ForEach(p =>
                {
                    if (!p.Value.Any())
                        return;

                    sb.AppendLine($"== EVENT: {p.Key} ({p.Value.Count} handler(s)) ==");

                    p.Value.ForEach(stats =>
                    {
                        sb.AppendLine($"    > {stats.Item1} = L: {stats.Item2} ms;S: {stats.Item3} ms;A: {stats.Item4} ms;LS: {stats.Item5} ms;TPS: {stats.Item6};NUM: {stats.Item7}");
                    });

                    p.Value.ReturnList();
                });

                dict.ReturnDictionary();

                var str = sb.ReturnStringBuilderValue();

                if (!string.IsNullOrWhiteSpace(str))
                    Plugin.Info(str);

                _registry.For((_, ev) => ev.Stats?.Reset());
            }
        }

        [Command("event", BetterCommands.CommandType.RemoteAdmin, BetterCommands.CommandType.GameConsole)]
        private static string EventCommand(ReferenceHub sender, string id)
        {
            if (id is "handlers")
            {
                var ordered = _registry.OrderByDescending(x => x.Type);
                var sb = Pools.PoolStringBuilder();

                ordered.ForEach(h => sb.AppendLine($"{h.Type} {h.Target.Method.ToLogName()} {h.Buffer?.Length ?? -1} {h.Buffer?.Count(x => x != null) ?? -1}"));

                return sb.ReturnStringBuilderValue();
            }

            if (id is "stats")
            {
                var sb = Pools.PoolStringBuilder();
                var dict = Pools.PoolDictionary<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>>();

                _registry.ForEach(ev =>
                {
                    if (ev.Stats is null
                    || ev.Stats.LongestTime == -1
                    || ev.Stats.ShortestTime == -1
                    || ev.Stats.AverageTime == -1
                    || ev.Stats.LastTime == -1
                    || ev.Stats.TicksWhenLongest <= 0
                    || ev.Stats.Executions <= 0)
                        return;

                    if (!dict.ContainsKey(ev.Type))
                        dict[ev.Type] = Pools.PoolList<Tuple<string, double, double, double, double, double, int>>();

                    dict[ev.Type].Add(new Tuple<string, double, double, double, double, double, int>(
                        ev.Target.Method.ToLogName(),
                        ev.Stats.LongestTime,
                        ev.Stats.ShortestTime,
                        ev.Stats.AverageTime,
                        ev.Stats.LastTime,
                        ev.Stats.TicksWhenLongest,
                        ev.Stats.Executions));
                });

                sb.AppendLine();

                dict.ForEach(p =>
                {
                    if (!p.Value.Any())
                        return;

                    sb.AppendLine($"== EVENT: {p.Key} ({p.Value.Count} handler(s)) ==");

                    p.Value.ForEach(stats =>
                    {
                        sb.AppendLine($"    > {stats.Item1} = L: {stats.Item2} ms;S: {stats.Item3} ms;A: {stats.Item4} ms;LS: {stats.Item5} ms;TPS: {stats.Item6};NUM: {stats.Item7}");
                    });

                    p.Value.ReturnList();
                });

                dict.ReturnDictionary();
                return sb.ReturnStringBuilderValue();
            }

            if (id is "log")
            {
                DebugOverride = !DebugOverride;
                return DebugOverride ? "Debug enabled" : "Debug disabled";
            }

            if (id is "reset")
            {
                _registry.ForEach(r => r.Stats.Reset());
                return "Stats reset.";
            }

            return "Unknown ID";
        }
    }
}