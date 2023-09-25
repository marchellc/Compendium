using BetterCommands;

using Compendium.Comparison;
using Compendium.Reflect.Dynamic;
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
        private static bool _everExecuted;
        private static List<EventRegistryData> _registry = new List<EventRegistryData>();

        public static List<ServerEventType> RecordEvents => Plugin.Config.EventSettings.RecordEvents;

        public static bool RoundSummary => Plugin.Config.EventSettings.ShowRoundSummary || DebugOverride;
        public static bool LogExecutionTime => Plugin.Config.EventSettings.ShowTotalExecution || DebugOverride;
        public static bool LogHandlers => Plugin.Config.EventSettings.ShowEventDuration || DebugOverride;

        public static bool DebugOverride;

        public static double HighestEventDuration => _registry.Where(x => x.Stats.LongestTime != -1).OrderByDescending(x => x.Stats.LongestTime).FirstOrDefault()?.Stats?.LongestTime ?? 0;
        public static double ShortestEventDuration => _registry.Where(x => x.Stats.ShortestTime != -1).OrderByDescending(x => x.Stats.ShortestTime).LastOrDefault()?.Stats?.ShortestTime ?? 0;
        public static double HighestTicksPerSecond => _registry.Where(x => x.Stats.TicksWhenLongest != 0).OrderByDescending(x => x.Stats.TicksWhenLongest).FirstOrDefault()?.Stats?.TicksWhenLongest ?? 0;

        [Load]
        private static void Initialize()
        {
            EventBridge.OnExecuting = OnExecutingEvent;
        }

        [Unload]
        private static void Unload()
        {
            EventBridge.OnExecuting = null;
            _registry.Clear();
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
            try
            {
                if (method.DeclaringType.Namespace.StartsWith("System"))
                    return;

                if (!EventUtils.TryCreateEventData(method, instance, out var data))
                    return;

                _registry.Add(data);
                Plugin.Info($"Registered event '{data.Type}' ({DynamicMethodDelegateFactory.GetMethodName(data.Target.Method)})");
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

            DynamicMethodDelegateFactory.MethodCache.TryGetValue(method, out method);
            return _registry.TryGetFirst(ev => ev.Target.Method == method && NullableObjectComparison.Compare(ev.Target.Target, instance), out _);
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

            DynamicMethodDelegateFactory.MethodCache.TryGetValue(method, out method);
            return _registry.RemoveAll(ev => ev.Target.Method == method && NullableObjectComparison.Compare(ev.Target.Target, instance)) > 0;
        }

        private static bool OnExecutingEvent(IEventArguments args, Event evInfo, ValueReference isAllowed)
        {
            if (!_registry.Any(ev => ev.Type == args.BaseType))
                return true;

            _everExecuted = true;

            var startTime = DateTime.Now;
            var list = _registry.Where(x => x.Type == args.BaseType);
            var result = true;

            foreach (var ev in list)
            {
                var startEv = DateTime.Now;

                EventUtils.TryInvoke(ev, args, isAllowed, out result);

                var endEv = DateTime.Now;
                var durationEv = TimeSpan.FromTicks((endEv - startEv).Ticks);

                ev.Stats.Record(durationEv.TotalMilliseconds);

                if (LogHandlers)
                    Plugin.Debug($"Finished executing '{ev.Type}' handler '{DynamicMethodDelegateFactory.GetMethodName(ev.Target.Method)}' in {durationEv.TotalMilliseconds} ms");
            }

            if (isAllowed.Value is null)
                isAllowed.Value = result;

            if (!LogExecutionTime)
                return result;

            var endTime = DateTime.Now;
            var duration = TimeSpan.FromTicks((endTime - startTime).Ticks);

            Plugin.Debug($"Total Event Execution of {args.BaseType} took {duration.TotalMilliseconds} ms; longest: {HighestEventDuration} ms; shortest: {ShortestEventDuration} ms");
            return result;
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
                    if (!dict.ContainsKey(ev.Type))
                        dict[ev.Type] = Pools.PoolList<Tuple<string, double, double, double, double, double, int>>();

                    if (ev.Stats is null
                    || ev.Stats.LongestTime == -1
                    || ev.Stats.ShortestTime == -1
                    || ev.Stats.AverageTime == -1
                    || ev.Stats.LastTime == -1
                    || ev.Stats.TicksWhenLongest <= 0
                    || ev.Stats.Executions <= 0)
                        return;

                    dict[ev.Type].Add(new Tuple<string, double, double, double, double, double, int>(
                        DynamicMethodDelegateFactory.GetMethodName(ev.Target.Method),
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
                    sb.AppendLine($"== EVENT: {p.Key} ({p.Value.Count} handler(s)) ==");

                    p.Value.ForEach(stats =>
                    {
                        sb.AppendLine($"    > {stats.Item1} = L: {stats.Item2} ms;S: {stats.Item2} ms;A: {stats.Item3} ms;LS: {stats.Item4} ms;TPS: {stats.Item5};NUM: {stats.Item6};{stats.Item7}");
                    });

                    p.Value.ReturnList();
                });

                dict.ReturnDictionary();

                Plugin.Info(sb.ReturnStringBuilderValue());

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

                ordered.ForEach(h => sb.AppendLine($"{h.Type} {DynamicMethodDelegateFactory.GetMethodName(h.Target.Method)} {h.Buffer?.Length ?? -1} {h.Buffer?.Count(x => x != null) ?? -1}"));

                return sb.ReturnStringBuilderValue();
            }

            if (id is "stats")
            {
                var sb = Pools.PoolStringBuilder();
                var dict = Pools.PoolDictionary<ServerEventType, List<Tuple<string, double, double, double, double, double, int>>>();

                _registry.ForEach(ev =>
                {
                    if (!dict.ContainsKey(ev.Type))
                        dict[ev.Type] = Pools.PoolList<Tuple<string, double, double, double, double, double, int>>();

                    if (ev.Stats is null
                    || ev.Stats.LongestTime == -1
                    || ev.Stats.ShortestTime == -1
                    || ev.Stats.AverageTime == -1
                    || ev.Stats.LastTime == -1
                    || ev.Stats.TicksWhenLongest <= 0
                    || ev.Stats.Executions <= 0)
                        return;

                    dict[ev.Type].Add(new Tuple<string, double, double, double, double, double, int>(
                        DynamicMethodDelegateFactory.GetMethodName(ev.Target.Method),
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
                    sb.AppendLine($"== EVENT: {p.Key} ({p.Value.Count} handler(s)) ==");

                    p.Value.ForEach(stats =>
                    {
                        sb.AppendLine($"    > {stats.Item1} = L: {stats.Item2} ms;S: {stats.Item2} ms;A: {stats.Item3} ms;LS: {stats.Item4} ms;TPS: {stats.Item5};NUM: {stats.Item6};{stats.Item7}");
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