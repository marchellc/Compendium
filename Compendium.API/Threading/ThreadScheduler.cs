using helpers.Attributes;
using helpers.Dynamic;
using helpers.Extensions;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;

using UnityEngine;

namespace Compendium.Threading
{
    public static class ThreadScheduler
    {
        private static Queue<Tuple<MethodInfo, object, object[]>> _queue = new Queue<Tuple<MethodInfo, object, object[]>>();
        private static Timer _timer;

        public static int Size => _queue.Count;

        [Load(Priority = helpers.Priority.Low)]
        public static void Load()
        {
            if (_timer != null)
                Unload();

            _timer = new Timer();
            _timer.Interval = Time.deltaTime * 1000f;
            _timer.Elapsed += OnElapsed;
            _timer.Enabled = true;
        }

        public static void Unload()
        {
            if (_timer is null)
                return;

            _timer.Enabled = false;
            _timer.Elapsed -= OnElapsed;
            _timer.Dispose();
            _timer = null;
        }

        public static void Schedule(MethodInfo target, object handle, params object[] args)
            => _queue.Enqueue(new Tuple<MethodInfo, object, object[]>(target, handle, args));

        private static void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            { 
                if (_queue.TryDequeue(out var tuple))
                {
                    try
                    {
                        tuple.Item1.InvokeDynamic(tuple.Item2, tuple.Item3);
                    }
                    catch (Exception exx)
                    {
                        Plugin.Error($"Failed to execute scheduled method '{tuple.Item1.ToLogName()}'");
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
    }
}