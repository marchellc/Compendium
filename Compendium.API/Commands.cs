using BetterCommands;

using Compendium.Helpers.Units;

using helpers.Extensions;

using PluginAPI.Core;

using System.Text;

namespace Compendium
{
    public static class Commands
    {
        [Command("units", CommandType.GameConsole, CommandType.RemoteAdmin)]
        [CommandAliases("unitlist")]
        [Description("Displays a list of all NTF units.")]
        public static string Units(Player sender)
        {
            var units = UnitHelper.NtfUnits;

            if (units is null || !units.Any())
                return "There aren't any active NTF units.";

            var sb = new StringBuilder();

            sb.AppendLine($"Showing {units.Count} NTF unit(s) ..");
            sb.AppendLine($"------------------------------------");

            for (int i = 0; i < units.Count; i++)
            {
                sb.AppendLine($"Unit {i}: {units[i]}");
            }

            return sb.ToString();
        }

        [Command("setunitid", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("sunitid", "setuid")]
        [Description("Sets the NTF unit ID of the targeted player.")]
        public static string SetUnitId(Player sender, Player target, byte unitId)
        {
            if (!UnitHelper.TrySetUnitId(target.ReferenceHub, unitId))
                return $"Failed to change {target.ReferenceHub.LoggedNameFromRefHub()}'s unit ID to {unitId}!";

            return $"Changed {target.ReferenceHub.LoggedNameFromRefHub()} unit ID to {unitId}!";
        }

        [Command("addunit", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("aunit", "addu")]
        [Description("Adds a new unit to the unit list.")]
        public static string AddUnit(Player sender, string unit)
        {
            if (!UnitHelper.TryCreateUnit(unit))
                return $"Failed to add NTF unit: {unit}";

            return $"Added NTF unit: {unit}";
        }

        [Command("setunit", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("sunit", "setu")]
        [Description("Sets the NTF unit of the targeted player.")]
        public static string SetUnit(Player sender, Player target, string unitName, bool addIfMissing = true)
        {
            if (!UnitHelper.TrySetUnitName(target.ReferenceHub, unitName, addIfMissing))
                return $"Failed to set unit of {target.ReferenceHub.LoggedNameFromRefHub()} to {unitName}!";

            return $"Set unit of {target.ReferenceHub.LoggedNameFromRefHub()} to {unitName}!";
        }
    }
}