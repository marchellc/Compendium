using BetterCommands;
using BetterCommands.Permissions;

using Compendium.PlayerData;
using Compendium.Staff;

namespace Compendium.Custom.Commands
{
    public static class StaffCommands
    {
        [Command("setgroup", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Sets the group of a specific player.")]
        [Permission(PermissionLevel.Administrator)]
        public static string SetGroupCommand(ReferenceHub sender, PlayerDataRecord target, StaffGroup group)
        {
            StaffHandler.SetGroup(target.UserId, group.Key);
            return $"Set group of '{target.NameTracking.LastValue ?? "unknown nick"}' to {group.Text} ({group.Key})";
        }

        [Command("delgroup", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Removes the group of a specific player.")]
        [Permission(PermissionLevel.Administrator)]
        public static string RemoveGroupCommand(ReferenceHub sender, PlayerDataRecord target, StaffGroup group)
        {
            StaffHandler.RemoveGroup(target.UserId, group.Key);
            return $"Removed group '{group.Text}' ({group.Key}) from '{target.NameTracking.LastValue ?? "unknown nick"}'";
        }
    }
}
