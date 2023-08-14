using Compendium.Commands.Groups;

namespace Compendium.Commands.Modules
{
    public class CommandModule : CommandGroup, ICommandModule 
    {
        private ICommandContext _ctx;

        public CommandModule() : base(null, null) { }

        public ICommandContext Context => _ctx;

        public bool IsExecuting => _ctx != null;

        internal void SetContext(ICommandContext ctx)
            => _ctx = ctx;
    }
}