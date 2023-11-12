using System;

namespace Compendium.Updating
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class UpdateAttribute : Attribute
    {
        public bool IsUnity { get; set; } = true;

        public bool PauseWaiting { get; set; } = true;
        public bool PauseRestarting { get; set; } = true;

        public int Delay { get; set; } = -1;
    }
}