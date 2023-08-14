using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class RemainderAttribute : Attribute
    {
    }
}