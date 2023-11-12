using Compendium.Comparison;
using helpers.Extensions;
using System;
using System.Reflection;

namespace Compendium.Updating
{
    public class UpdateData
    {
        public UpdateCall CallType { get; } = UpdateCall.WithParameter;

        public DateTime LastCallTime { get; internal set; } = DateTime.Now;

        public int DelayTime { get; set; } = -1;

        public bool IsMeasured { get; set; }
        public bool IsEverMeasured { get; private set; }

        public bool IsUnity { get; } = true;

        public bool PauseWaiting { get; } = true;
        public bool PauseRestarting { get; } = true;

        public double LastCall { get; set; } = 0;
        public double LongestCall { get; set; } = 0;
        public double ShortestCall { get; set; } = 0;
        public double AverageCall => (LongestCall + ShortestCall) / 2;

        public Action ParameterlessCall { get; }
        public Action<UpdateData> ParameterCall { get; }

        public UpdateData(bool isUnity, bool isWaiting, bool isRestarting, int delayTime, Action parameterlessCall)
        {
            IsUnity = isUnity;

            PauseWaiting = isWaiting;
            PauseRestarting = isRestarting;

            CallType = UpdateCall.WithoutParameter;

            DelayTime = delayTime;
            ParameterlessCall = parameterlessCall;

#if DEBUG
            IsMeasured = true;
#endif
        }

        public UpdateData(bool isUnity, bool isWaiting, bool isRestarting, int delayTime, Action<UpdateData> parameterCall)
        {
            IsUnity = isUnity;

            PauseWaiting = isWaiting;
            PauseRestarting = isRestarting;

            CallType = UpdateCall.WithParameter;

            DelayTime = delayTime;
            ParameterCall = parameterCall;

#if DEBUG
            IsMeasured = true;
#endif
        }

        public bool CanRun()
            => DelayTime <= 0 || (DateTime.Now - LastCallTime).TotalMilliseconds >= DelayTime;

        public bool Is(MethodBase method, object target)
        {
            if (ParameterCall != null)
                return ParameterCall.Method == method && NullableObjectComparison.Compare(target, ParameterCall.Target);

            if (ParameterlessCall != null)
                return ParameterlessCall.Method == method && NullableObjectComparison.Compare(target, ParameterlessCall.Target);

            return false;
        }

        public void DoCall()
        {
            if (!CanRun())
                return;

            /*
            if (ParameterCall != null)
                Plugin.Debug($"Calling {ParameterCall.Method.ToLogName()}, {LastCall} ms / {LongestCall} ms / {ShortestCall} ms");
            else if (ParameterlessCall != null)
                Plugin.Debug($"Calling {ParameterlessCall.Method.ToLogName()}, {LastCall} ms / {LongestCall} ms / {ShortestCall} ms");
            */

            LastCallTime = DateTime.Now;

            try
            {
                if (IsMeasured)
                {
                    var start = DateTime.Now;

                    if (CallType is UpdateCall.WithoutParameter)
                        ParameterlessCall();
                    else
                        ParameterCall(this);

                    var time = (DateTime.Now - start).TotalMilliseconds;

                    LastCall = time;

                    if (LastCall > LongestCall)
                        LongestCall = time;

                    if (LastCall < ShortestCall)
                        ShortestCall = time;
                }
                else
                {
                    if (CallType is UpdateCall.WithoutParameter)
                        ParameterlessCall();
                    else
                        ParameterCall(this);
                }
            }
            catch (Exception ex)
            {
                Plugin.Error("Failed to invoke update");
                Plugin.Error(ex);
            }
        }
    }
}