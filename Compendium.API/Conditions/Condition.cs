using System;

namespace Compendium.Conditions
{
    public class Condition
    {
        public Predicate<ReferenceHub> Predicate { get; } 
        public Func<ReferenceHub, bool> Function { get; }

        public Condition()
        {
            Predicate = IsMatch;
            Function = IsMatch;
        }

        public virtual bool IsMatch(ReferenceHub hub) { return false; }
    }
}