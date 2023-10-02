using helpers;
using helpers.Dynamic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Compendium.Threading
{
    public class ThreadHandler : DisposableBase
    {
        private Thread _thread;

        public bool IsRunning => _thread.ThreadState == ThreadState.Running;

        public ThreadPriority Priority
        {
            get => _thread.Priority;
            set => _thread.Priority = value;
        }

        public ThreadHandler(ThreadStart threadStart)
            => _thread = new Thread(threadStart);

        public ThreadHandler(ParameterizedThreadStart threadStart)
            => _thread = new Thread(threadStart);

        public void Start()
        {
            try { _thread.Start(); } catch { }
        }

        public void Cancel()
            => Dispose();

        public override void Dispose()
        {
            base.Dispose();

            _thread.Interrupt();
            _thread.Join();
            _thread = null;
        }

        public static ThreadHandler Start(ThreadStart thread)
        {
            var handler = new ThreadHandler(thread);
            handler.Start();
            return handler;
        }

        public static ThreadHandler Start(ParameterizedThreadStart threadStart)
        {
            var handler = new ThreadHandler(threadStart);
            handler.Start();
            return handler;
        }

        public static ThreadHandler StartRepeating(MethodInfo method, int interval, object handle = null, params object[] args)
            => Start(async () =>
            {
                while (true)
                {
                    method.InvokeDynamic(handle, args);
                    await Task.Delay(interval);
                }
            });
    }
}
