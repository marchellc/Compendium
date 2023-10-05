using System;

namespace Compendium.Scheduling.Update
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class UpdateAttribute : Attribute
    {
        public UpdateSchedulerType Type { get; set; } = UpdateSchedulerType.UnityThread;

        public int Delay { get; set; } = -1;

        public bool DisableUnityCheck { get; set; }
    }
}