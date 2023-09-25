using helpers.Attributes;
using helpers.Extensions;

using MEC;

using System;
using System.Collections.Generic;

namespace Compendium.Threading
{
    public static class ThreadScheduler
    {
        private static Queue<Tuple<Delegate, object[]>> _queue = new Queue<Tuple<Delegate, object[]>>();
        private static CoroutineHandle _handle;

        public static int Size => _queue.Count;

        [Load(Priority = helpers.Priority.Low)]
        public static void Load()
        {
            _handle = Timing.RunCoroutine(Handle());
        }

        public static void Unload()
        {
            Timing.KillCoroutines(_handle);
        }

        public static void Schedule(Delegate target, params object[] args)
        {
            _queue.Enqueue(new Tuple<Delegate, object[]>(target, args));
        }

        private static IEnumerator<float> Handle()
        {
            while (true)
            {
                yield return Timing.WaitForOneFrame;

                if (_queue.TryDequeue(out var del))
                {
                    try
                    {
                        del.Item1.Method.Invoke(del.Item1.Target, del.Item2);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Error($"Failed to invoke scheduled delegate '{del.Item1.Method.ToLogName()}'");
                        Plugin.Error(ex);
                    }
                }
            }
        }
    }
}