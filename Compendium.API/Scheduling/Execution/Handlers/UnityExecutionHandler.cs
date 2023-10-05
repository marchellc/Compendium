using helpers.Extensions;

using System;

namespace Compendium.Scheduling.Execution.Handlers
{
    public class UnityExecutionHandler : ExecutionHandler
    {
        public override void Execute(ExecutionData data)
        {
            try
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
            catch (Exception ex)
            {
                InvokeCallback(data.Callback, null, ex, 0);

                Plugin.Error($"Unity Execution Handler failed to invoke delegate '{data.Target.Method.ToLogName()}' due to an exception:");
                Plugin.Error(ex);
            }
        }
    }
}