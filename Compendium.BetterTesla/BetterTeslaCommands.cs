using BetterCommands;

using Compendium.Extensions;
using Compendium.Calls;

using PlayerRoles;

using PluginAPI.Core;

using System.Linq;

namespace Compendium.BetterTesla
{
    public static class BetterTeslaCommands
    {
        [Command("teslatp", CommandType.RemoteAdmin)]
        public static string TeslaTp(Player sender)
        {
            var teslas = TeslaGateController.Singleton.TeslaGates;
            var sorted = teslas.OrderByDescending(tesla => tesla.DistanceSquared(sender.Position));
            var result = teslas.First();

            sender.IsGodModeEnabled = true;
            sender.Position = result.Position;

            CallHelper.CallWithDelay(() => sender.IsGodModeEnabled = false, 2f);

            return $"Teleported you to the nearest tesla gate ({result.Room.Name})";
        }

        [Command("teslarole", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("trole", "teslar")]
        public static string SwitchRole(Player sender, RoleTypeId role)
        {
            if (BetterTeslaLogic.RoundDisabledRoles.Contains(role) && BetterTeslaLogic.RoundDisabledRoles.Remove(role))
            {
                return $"Re-enabled Tesla gates for role: {role}";
            }
            else
            {
                BetterTeslaLogic.RoundDisabledRoles.Add(role);
                return $"Disabled Tesla gates for role: {role}";
            }
        }

        [Command("teslastatus", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("teslas", "tstatus")]
        public static string SwitchTesla(Player sender)
        {
            BetterTeslaLogic.RoundDisabled = !BetterTeslaLogic.RoundDisabled;

            return BetterTeslaLogic.RoundDisabled ?
                "Tesla Gates disabled." :
                "Tesla Gates enabled.";
        }
    }
}