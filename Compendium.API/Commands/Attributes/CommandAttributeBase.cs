using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandAttributeBase : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual CommandSource Source { get; }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return false;

            if (string.IsNullOrWhiteSpace(Description))
                Description = "Missing description.";

            return true;
        }
    }
}
