using Compendium.Update;

using helpers.Attributes;
using helpers.Dynamic;
using helpers.Extensions;

using System;
using System.Collections.Generic;

using UnityEngine;

using Timer = System.Timers.Timer;

namespace Compendium.Threading
{
    public static class ThreadScheduler
    {
        private static Queue<Tuple<DynamicMethodDelegate, object, object[]>> _queue = new Queue<Tuple<DynamicMethodDelegate, object, object[]>>();
        private static Timer _timer;

        public static int Size => _queue.Count;

        [Load(Priority = helpers.Priority.Low)]
        public static void Load()
        {
            UpdateSynchronizer.OnUpdate += OnUpdate;
        }

        [Unload]
        public static void Unload()
        {
            UpdateSynchronizer.OnUpdate -= OnUpdate;
        }

        public static void Schedule(DynamicMethodDelegate target, object handle, params object[] args)
            => _queue.Enqueue(new Tuple<DynamicMethodDelegate, object, object[]>(target, handle, args));

        private static void OnUpdate()
        {
            if (_queue is null)
                return;

            try
            {
                if (_queue.TryDequeue(out var tuple))
                {
                    if (tuple is null || tuple.Item1 is null)
                        return;

                    try
                    {
                        tuple.Item1(tuple.Item2, tuple.Item3);
                    }
                    catch (Exception exx)
                    {
                        Plugin.Error($"Failed to execute scheduled method '{tuple.Item1.Method.ToLogName()}'");
                        Plugin.Error(exx);
                    }
                }

                if (_timer != null)
                    _timer.Interval = Time.deltaTime * 1000f;
            }
            catch (Exception ex)
            {
                Plugin.Error(ex);
            }
        }

        /*
        private static void OnElapsed(object sender, ElapsedEventArgs e)
        {
            if (_queue is null)
                return;

            try
            { 
                if (_queue.TryDequeue(out var tuple))
                {
                    try
                    {
                        tuple.Item1(tuple.Item2, tuple.Item3);
                    }
                    catch (Exception exx)
                    {
                        Plugin.Error($"Failed to execute scheduled method '{tuple.Item1.Method.ToLogName()}'");
                        Plugin.Error(exx);
                    }
                }

                if (_timer != null)
                    _timer.Interval = Time.deltaTime * 1000f;
            }
            catch (Exception ex)
            {
                Plugin.Error(ex);
            }
        }
        */
    }
}