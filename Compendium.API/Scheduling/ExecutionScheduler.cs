using System;
using System.Collections.Generic;
using System.Reflection;

using Compendium.Scheduling.Execution.Handlers;
using Compendium.Timing;

using helpers;
using helpers.Attributes;
using helpers.Dynamic;

namespace Compendium.Scheduling.Execution
{
    public static class ExecutionScheduler
    {
        private static readonly List<ExecutionData> _executionStack = new List<ExecutionData>();
        private static readonly object _lock = new object();

        public static readonly ExecutionHandler UnityHandler = new UnityExecutionHandler();
        public static readonly ExecutionHandler SideHandler = new SideExecutionHandler();
        public static readonly ExecutionHandler NewHandler = new NewExecutionHandler();

        [Load]
        private static void Load()
            => ThreadSafeTimer.Create(10, OnTick);

        public static void Schedule(Type type, string methodName, object handle, object[] args, DateTime? scheduledAt, int? repeatCount, bool shouldMeasure, ExecutionThread threadType)
        {
            var method = type.Method(methodName);

            if (method is null)
                return;

            Schedule(method, handle, args, scheduledAt, repeatCount, shouldMeasure, threadType);
        }

        public static void ScheduleStatic(MethodInfo method, ExecutionThread thread, bool measure, params object[] args)
            => Schedule(method, null, args, null, null, measure, thread);

        public static void ScheduleStaticDelayed(MethodInfo method, ExecutionThread thread, int msDelay, bool measure, params object[] args)
            => Schedule(method, null, args, DateTime.Now + TimeSpan.FromMilliseconds(msDelay), null, measure, thread);

        public static void ScheduleStaticRepeated(MethodInfo method, ExecutionThread thread, int count, bool measure, params object[] args)
            => Schedule(method, null, args, null, count, measure, thread);

        public static void ScheduleStaticRepeatedDelayed(MethodInfo method, ExecutionThread thread, int msDelay, int count, bool measure, params object[] args)
            => Schedule(method, null, args, DateTime.Now + TimeSpan.FromMilliseconds(msDelay), count, measure, thread);

        public static void ScheduleDelayed(MethodInfo method, object handle, ExecutionThread thread, int msDelay, bool measure, params object[] args)
            => Schedule(method, handle, args, DateTime.Now + TimeSpan.FromMilliseconds(msDelay), null, measure, thread);

        public static void ScheduleRepeated(MethodInfo method, object handle, ExecutionThread thread, int count, bool measure, params object[] args)
            => Schedule(method, handle, args, null, count, measure, thread);

        public static void ScheduleRepeatedDelayed(MethodInfo method, object handle, ExecutionThread thread, int count, int msDelay, bool measure, params object[] args)
            => Schedule(method, handle, args, DateTime.Now + TimeSpan.FromMilliseconds(msDelay), count, measure, thread);

        public static void Schedule(MethodInfo method, object handle, object[] args, DateTime? scheduledAt, int? repeatCount, bool shouldMeasure, ExecutionThread threadType)
            => Schedule(method.GetOrCreateInvoker(), handle, args, scheduledAt, repeatCount, shouldMeasure, threadType);

        public static void Schedule(DynamicMethodDelegate target, object handle, object[] args, DateTime? scheduledAt, int? repeatCount, bool shouldMeasure, ExecutionThread threadType)
        {
            lock (_lock)
                _executionStack.Add(new ExecutionData(target, handle, args, DateTime.Now, scheduledAt, repeatCount, shouldMeasure, threadType));
        }

        private static void OnTick()
        {
            lock (_lock)
            {
                if (_executionStack.Count > 0)
                {
                    var invoked = Pools.PoolList<ExecutionData>();

                    for (int i = 0; i < _executionStack.Count; i++)
                    {
                        var data = _executionStack[i];

                        if (data.At.HasValue && data.At.Value < DateTime.Now)
                            continue;

                        if (data.Repeat.HasValue
                            && data.Repeat.Value > 0
                            && data.Thread == ExecutionThread.Unity)
                        {
                            for (int x = 0; x < data.Repeat.Value; x++)
                                TickInvoke(data);

                            invoked.Add(data);
                        }
                        else
                        {
                            TickInvoke(data);
                            invoked.Add(data);
                        }
                    }

                    invoked.For((_, data) => _executionStack.Remove(data));
                    invoked.ReturnList();
                }
            }
        }

        private static void TickInvoke(ExecutionData data)
        {
            switch (data.Thread)
            {
                case ExecutionThread.Unity:
                    UnityHandler.Execute(data);
                    return;

                case ExecutionThread.Side:
                    SideHandler.Execute(data);
                    return;

                case ExecutionThread.New:
                    NewHandler.Execute(data);
                    return;
            }
        }
    }
}