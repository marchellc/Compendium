using PluginAPI.Core;

namespace Compendium.Commands
{
    public interface ICommandContext
    {
        string Query { get; }
        string Command { get; }

        Player Player { get; }
        ReferenceHub Hub { get; }

        CommandSource Source { get; }

        bool IsServer { get; }
        bool IsPlayer { get; }

        bool HasResponded { get; }

        void Respond(object response, bool isSuccess);
    }
}