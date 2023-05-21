using CommandSystem;

using Compendium.Helpers.Commands;
using Compendium.Helpers.Timing;

using System;

namespace Compendium.Commands.Timing
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    public class FrameTimingsCommand : ICommand, IUsageProvider
    {
        public string Command { get; } = "frame";
        public string[] Aliases { get; } = CommandHelper.EmptyAliases;
        public string Description { get; } = "Shows frame timings.";
        public string[] Usage { get; } = new string[] { "frame" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = FrameUpdateHelper.CreateReport();
            return true;
        }
    }
}