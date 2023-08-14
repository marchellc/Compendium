using helpers;

using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandPriorityAttribute : Attribute
    {
        public Priority Priority { get; set; } = Priority.Normal;
    }
}