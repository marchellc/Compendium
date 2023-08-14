using Compendium.Commands.Parameters;

using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public class RestrictionAttribute : Attribute
    {
        public IParameterRestriction Restriction { get; }

        public RestrictionAttribute(IParameterRestriction restriction)
            => Restriction = restriction;
    }
}