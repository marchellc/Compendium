using helpers.Dynamic;

using System;

namespace Compendium.Scheduling.Execution
{
    public class ExecutionHandler
    {
        public virtual void Execute(ExecutionData data) => Execute(data.Target, data.Handle, data.Args, data.Callback);
        public virtual void Execute(DynamicMethodDelegate del, object handle, object[] args, Action<ExecutionResult> callback) { }

        public void InvokeCallback(Action<ExecutionResult> callback, object value, Exception exception, double time)
        {
            if (callback is null)
                return;

            var callbackArray = new object[1];
            callbackArray[0] = new ExecutionResult(value, exception is null, exception, time);
            ExecutionScheduler.Schedule(callback.Method, callback.Target, callbackArray, null, null, false, ExecutionThread.Unity);
        }
    }
}