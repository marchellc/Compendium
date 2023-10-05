using helpers.Extensions;

using System;
using System.Threading;

namespace Compendium.Scheduling.Execution.Handlers
{
    public class NewExecutionHandler : ExecutionHandler
    {
        public override void Execute(ExecutionData data)
        {
            CreateThread(data).Start();
        }

        private Thread CreateThread(ExecutionData data)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    if (data.Repeat.HasValue && data.Repeat.Value > 1)
                    {
                        for (int i = 0; i < data.Repeat.Value; i++)
                            SideInvoke(data);
                    }
                    else
                    {
                        SideInvoke(data);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed to execute scheduled execution on a new thread ('{data.Target.Method.ToLogName()}') due to an exception:");
                    Plugin.Error(ex);

                    InvokeCallback(data.Callback, null, ex, 0);
                }
            });

            thread.Priority = ThreadPriority.Lowest;

            return thread;
        }

        private void SideInvoke(ExecutionData data)
        {
            if (data.IsMeasured)
            {
                var start = DateTime.Now;
                var value = data.Target(data.Handle, data.Args);
                var end = DateTime.Now;
                var ms = (end - start).Ticks / TimeSpan.TicksPerMillisecond;

                InvokeCallback(data.Callback, value, null, ms);
            }
            else
            {
                InvokeCallback(data.Callback, data.Target(data.Handle, data.Args), null, 0);
            }
        }
    }
}