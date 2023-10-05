using Compendium.Scheduling.Execution;
using Compendium.Scheduling.Update;
using Compendium.Comparison;
using Compendium.Attributes;
using Compendium.Timing;

using helpers.Dynamic;
using helpers.Extensions;
using helpers.Attributes;
using helpers.CustomReflect;
using helpers;

using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace Compendium.Scheduling
{
    public static class UpdateScheduler
    {
        private static readonly List<UpdateSchedulerData> _scheduledUpdates = new List<UpdateSchedulerData>();
        private static readonly object _lock = new object();

        [Load(Priority = Priority.Highest)]
        private static void Load()
        {
            ThreadSafeTimer.Create(10, OnTick);

            AttributeRegistryEvents.OnAttributeAdded += OnAttributeAdded;
            AttributeRegistryEvents.OnAttributeRemoved += OnAttributeRemoved;
        }


        public static void Register(MethodInfo target, int delay, UpdateSchedulerType type, params object[] args)
            => Register(target, null, delay, type, false, args);

        public static void Register(MethodInfo target, UpdateSchedulerType type, params object[] args)
            => Register(target, null, -1, type, false, args);

        public static void Register(MethodInfo target, object handle, UpdateSchedulerType type, params object[] args)
            => Register(target, handle, -1, type, false, args);

        public static void Register(MethodInfo target, object handle, int delay, UpdateSchedulerType type, bool skipUnity, params object[] args)
        {
            if (_scheduledUpdates.TryGetFirst(x => x.Target == target 
                            && NullableObjectComparison.Compare(x.Handle, handle)
                            && type == x.Type, out _))
                return;

            if (type != UpdateSchedulerType.UnityThread && !skipUnity)
            {
                var calls = MethodBodyReader.GetMethodCalls(target);
                var unityCalls = calls.Where(x => Reflection.HasType<UnityEngine.Object>(x.DeclaringType) || x.DeclaringType.Assembly.FullName.Contains("Assembly-CSharp"));

                if (unityCalls.Any())
                {
                    Plugin.Warn($"Method '{target.ToLogName()}' will not be registered as it contains Unity Engine calls in it's thread-unsafe code! Displaying found calls ..");
                    unityCalls.For((_, call) => Plugin.Warn($"[{_}]: {call.ToLogName()}"));
                    return;
                }
            }

            lock (_lock)
                _scheduledUpdates.Add(new UpdateSchedulerData(target, type, handle, args, delay));

            Plugin.Info($"Registered update function '{target.ToLogName()}', with {delay} ms delay and {type} type with {args.Length} args.");
        }

        public static void Unregister(MethodInfo target, UpdateSchedulerType type)
            => Unregister(target, null, type);

        public static void Unregister(MethodInfo target, object handle, UpdateSchedulerType type)
        {
            lock (_lock)
                _scheduledUpdates.RemoveAll(x => x.Target == target
                                    && NullableObjectComparison.Compare(x.Handle, handle)
                                    && x.Type == type);
        }

        private static void OnAttributeAdded(Attribute attribute, Type type, MemberInfo member, object handle)
        {
            if (member is null || !(member is MethodInfo method) || !(attribute is UpdateAttribute updateAttribute))
                return;

            if (!method.IsStatic)
                Register(method, handle, updateAttribute.Delay, updateAttribute.Type, updateAttribute.DisableUnityCheck);
            else
                Register(method, null, updateAttribute.Delay, updateAttribute.Type, updateAttribute.DisableUnityCheck);
        }

        private static void OnAttributeRemoved(Attribute attribute, Type type, MemberInfo member, object handle)
        {
            if (member is null || !(member is MethodInfo method) || !(attribute is UpdateAttribute updateAttribute))
                return;

            if (!method.IsStatic)
                Unregister(method, handle, updateAttribute.Type);
            else
                Unregister(method, updateAttribute.Type);
        }

        private static void OnTick()
        {
            if (!RoundHelper.IsReady)
                return;

            lock (_lock)
            {
                if (_scheduledUpdates.Count <= 0)
                    return;

                for (int i = 0; i < _scheduledUpdates.Count; i++)
                {
                    var data = _scheduledUpdates[i];

                    if (data.Delay > 0 && (DateTime.Now - data.LastCall).TotalMilliseconds < data.Delay)
                        continue;

                    data.LastCall = DateTime.Now;

                    switch (data.Type)
                    {
                        case UpdateSchedulerType.UnityThread:
                            data.Target.InvokeDynamic(data.Handle, data.Args);
                            return;

                        case UpdateSchedulerType.SideThread:
                            ExecutionScheduler.ScheduleStatic(data.Target, ExecutionThread.Side, false, data.Args);
                            return;

                        case UpdateSchedulerType.LoneThread:
                            ExecutionScheduler.ScheduleStatic(data.Target, ExecutionThread.New, false, data.Args);
                            return;
                    }
                }
            }
        }
    }
}