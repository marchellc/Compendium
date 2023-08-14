using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IgnoreExtraArgumentsAttribute : Attribute
    {
    }
}