using System;
using System.Reflection;

namespace Compendium.Scheduling.Update
{
    public class UpdateSchedulerData
    {
        public MethodInfo Target;

        public DateTime LastCall;

        public UpdateSchedulerType Type;

        public object Handle;
        public object[] Args;

        public int Delay;

        public UpdateSchedulerData(MethodInfo target, UpdateSchedulerType type, object handle, object[] args, int delay)
        {
            Target = target;
            Type = type;
            Handle = handle;
            Args = args;
            Delay = delay;
            LastCall = DateTime.Now;
        }
    }
}