using helpers.Dynamic;

using System;
using System.ComponentModel;

namespace Compendium.Timing
{
    public class ThreadSafeSynchronizer : ISynchronizeInvoke
    {
        public bool InvokeRequired { get; } = true;

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
            => throw new NotImplementedException();

        public object EndInvoke(IAsyncResult result)
            => throw new NotImplementedException();

        public object Invoke(Delegate method, object[] args)
        {
            var invoke = method.Method.GetOrCreateInvoker();

            if (invoke is null)
                throw new Exception($"Failed to create method invoker.");

            return invoke(method.Target, args);
        }

        public static readonly ThreadSafeSynchronizer Instance = new ThreadSafeSynchronizer();
    }
}