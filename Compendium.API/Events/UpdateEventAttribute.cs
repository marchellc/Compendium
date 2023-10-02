using Compendium.Update;

using System;

namespace Compendium.Events
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class UpdateEventAttribute : Attribute
    {
        public bool IsMainThread { get; set; } 

        public int TickRate { get; set; } = 10;

        public UpdateHandlerType Type { get; set; } = UpdateHandlerType.Engine;
    }
}