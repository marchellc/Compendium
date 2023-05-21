using CommandSystem;

using Compendium.Helpers.Commands;
using Compendium.Helpers.Timing;

using System;

namespace Compendium.Commands.Timing
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(ClientCommandHandler))]
    public class EventTimingsCommand : ICommand, IUsageProvider
    {
        public string Command { get; } = "event";
        public string[] Aliases { get; } = CommandHelper.EmptyAliases;
        public string Description { get; } = "Shows event timings.";
        public string[] Usage { get; } = new string[] { "event" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = EventTimingHelper.CreateReport();
            return true;
        }
    }
}