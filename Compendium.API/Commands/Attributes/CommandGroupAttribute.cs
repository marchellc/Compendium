using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CommandGroupAttribute : Attribute
    {
        public string Group { get; set; }
    }
}