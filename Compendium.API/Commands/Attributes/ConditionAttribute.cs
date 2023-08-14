using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class ConditionAttribute : Attribute
    {
        public ICondition Condition { get; }

        public ConditionAttribute(ICondition condition)
            => Condition = condition;
    }
}
