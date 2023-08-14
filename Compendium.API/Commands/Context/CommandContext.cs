using PluginAPI.Core;

using System;
using System.Linq;

namespace Compendium.Commands.Context
{
    public class CommandContext : ICommandContext
    {
        private bool _hasResponded;

        public string Query { get; }
        public string Command { get; }

        public Player Player { get; }
        public ReferenceHub Hub { get; }

        public CommandSource Source { get; }

        public bool IsServer { get; }
        public bool IsPlayer { get; }

        public bool HasResponded => _hasResponded;

        public CommandContext(ReferenceHub sender, CommandSource source, string query, string cmd)
        {
            Query = query;
            Command = cmd;

            Player = Player.Get(sender);
            Hub = sender;

            Source = source;

            IsServer = Hub.IsServer();
            IsPlayer = Hub.IsPlayer();
        }

        public void Respond(object response, bool isSuccess)
        {
            if (Hub is null)
                return;

            if (_hasResponded)
            {
                Plugin.Warn($"Command \"{Command}\" tried to send a second response!");
                return;
            }

            if (IsServer)
                ServerConsole.AddLog($"[{Command}] {response}", isSuccess ? ConsoleColor.Green : ConsoleColor.Red);
            else
            {
                Hub.characterClassManager.ConsolePrint($"[{Command}] {response}", isSuccess ? "green" : "red");
                Hub.queryProcessor.TargetReply(Hub.connectionToClient, $"{Command}#{response}", isSuccess, true, string.Empty);
            }

            _hasResponded = true;
        }
    }
}