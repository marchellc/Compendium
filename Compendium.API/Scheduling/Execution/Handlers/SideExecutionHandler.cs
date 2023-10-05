using System.Collections.Concurrent;
using System.Threading;
using System;

using helpers.Extensions;

namespace Compendium.Scheduling.Execution.Handlers
{
    public class SideExecutionHandler : ExecutionHandler
    {
        private ConcurrentQueue<ExecutionData> _threadQueue = new ConcurrentQueue<ExecutionData>();

        private Thread _thread;

        private CancellationToken _token;
        private CancellationTokenSource _source;

        private volatile object _lock = new object();

        public SideExecutionHandler()
        {
            _source = new CancellationTokenSource();
            _token = _source.Token;

            _thread = new Thread(Thread);
            _thread.Priority = ThreadPriority.AboveNormal;
            _thread.Start();
        }

        public override void Execute(ExecutionData data)
        {
            lock (_lock)
                _threadQueue.Enqueue(data);
        }

        private void Thread()
        {
            while (!_token.IsCancellationRequested)
            {
                lock (_lock)
                {
                    if (_threadQueue.TryDequeue(out var data))
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
                            Plugin.Error($"Failed to execute queued method '{data.Target.Method.ToLogName()}' due to an exception:");
                            Plugin.Error(ex);

                            InvokeCallback(data.Callback, null, ex, 0);
                        }
                    }    
                }
            }
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