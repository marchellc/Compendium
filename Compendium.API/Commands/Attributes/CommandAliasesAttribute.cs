using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandAliasesAttribute : Attribute
    {
        public string[] Aliases { get; }

        public CommandAliasesAttribute(params string[] aliases)
            => Aliases = aliases;
    }
}