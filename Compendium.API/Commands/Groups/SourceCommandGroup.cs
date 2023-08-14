using helpers.Extensions;

namespace Compendium.Commands.Groups
{
    public class SourceCommandGroup : CommandGroup
    {
        public CommandSource Source { get; }

        public SourceCommandGroup(CommandSource source) : base(source.ToString().SpaceByPascalCase(), null)
        {
            Source = source;
        }
    }
}
