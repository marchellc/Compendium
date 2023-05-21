using CommandSystem;

using System;

using Compendium.Helpers.Commands;

namespace Compendium.Commands.Timing
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    public class ShowTimingsCommandParent : ParentCommand, IUsageProvider
    {
        public ShowTimingsCommandParent() => LoadGeneratedCommands();

        public override string Command { get; } = "timings";
        public override string[] Aliases { get; } = Array.Empty<string>();
        public override string Description { get; } = "Shows Compendium execution timings.";
        public string[] Usage => new string[] { "%timing_type%" };

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new EventTimingsCommand());
            RegisterCommand(new FrameTimingsCommand());

            CommandHelper.AddUsageReplacement("%timing_type%", "The timing type (event/frame)");
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response) => this.ReturnUsage(out response);
    }
}