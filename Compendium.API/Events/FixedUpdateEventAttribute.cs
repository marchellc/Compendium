using System;

namespace Compendium.Events
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class FixedUpdateEventAttribute : Attribute { }
}