using BetterCommands;

using Compendium.Extensions;

using helpers.Extensions;

using PluginAPI.Core;

using Respawning.NamingRules;

using System.Text;

namespace Compendium
{
    public static class Commands
    {
        [Command("units", CommandType.GameConsole, CommandType.RemoteAdmin)]
        [CommandAliases("unitlist")]
        public static string Units(Player sender)
        {
            if (!UnitNameMessageHandler.ReceivedNames.Any())
                return "There aren't any active NTF units.";

            if (!UnitNameMessageHandler.ReceivedNames.TryGetValue(Respawning.SpawnableTeamType.NineTailedFox, out var units) || units.IsEmpty())
                return "There aren't any active NTF units.";

            var sb = new StringBuilder();

            sb.AppendLine($"Showing {units.Count} NTF unit(s) ..");
            sb.AppendLine();

            for (int i = 0; i < units.Count; i++)
            {
                sb.AppendLine($"Unit {i}: {units[i]}");
            }

            return sb.ToString();
        }

        [Command("setunitid", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("sunitid", "setuid")]
        public static string SetUnit(Player sender, Player target, byte unitId)
        {
            if (!UnitNameMessageHandler.ReceivedNames.Any())
                return "There aren't any active NTF units.";

            if (!UnitNameMessageHandler.ReceivedNames.TryGetValue(Respawning.SpawnableTeamType.NineTailedFox, out var units) || units.IsEmpty())
                return "There aren't any active NTF units.";

            if (unitId >= units.Count)
                return "Provided unit ID is out of range.";

            var unitName = units[unitId];

            target.ReferenceHub.SetUnit(unitName);

            return $"Changed {target.ReferenceHub.LoggedNameFromRefHub()} unit to {unitName} ({unitId})!";
        }
    }
}