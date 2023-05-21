using System;

namespace Compendium.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InitOnLoadAttribute : Attribute 
    {
        public int Priority { get; set; } = -1;
    }
}