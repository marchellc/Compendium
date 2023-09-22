using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Staff
{
    public static class StaffUtils
    {
        public static IReadOnlyList<PlayerPermissions> Permissions { get; } = Enum.GetValues(typeof(PlayerPermissions))
            .Cast<PlayerPermissions>()
            .ToList();

        public static PlayerPermissions ToNwPermissions(StaffGroup group)
        {
            var list = Permissions;

            PlayerPermissions perms = default;

            foreach (var perm in list)
            {
                if (HasPermission(group, perm))
                    perms |= perm;
            }

            return perms;
        }

        public static bool HasPermission(StaffGroup group, PlayerPermissions playerPermissions)
        {
            if (group.Permissions.Contains(StaffPermissions.Override))
                return true;

            switch (playerPermissions)
            {
                case PlayerPermissions.Noclip:
                    return group.GroupFlags.Contains(StaffGroupFlags.IsNoClip);

                case PlayerPermissions.BanningUpToDay:
                    return group.Permissions.Contains(StaffPermissions.DayBans);

                case PlayerPermissions.AFKImmunity:
                    return group.GroupFlags.Contains(StaffGroupFlags.IsAfkImmune);

                case PlayerPermissions.SetGroup:
                    return group.Permissions.Contains(StaffPermissions.ServerConfigs);

                case PlayerPermissions.AdminChat:
                    return group.GroupFlags.Contains(StaffGroupFlags.IsAdminChat);

                case PlayerPermissions.ServerConfigs:
                    return group.Permissions.Contains(StaffPermissions.ServerConfigs);

                case PlayerPermissions.Announcer:
                    return group.Permissions.Contains(StaffPermissions.CassieAccess);

                case PlayerPermissions.Broadcasting:
                    return group.Permissions.Contains(StaffPermissions.BroadcastAccess);

                case PlayerPermissions.Effects:
                    return group.Permissions.Contains(StaffPermissions.PlayerManagement);

                case PlayerPermissions.FacilityManagement:
                    return group.Permissions.Contains(StaffPermissions.MapManagement);

                case PlayerPermissions.ForceclassSelf:
                    return group.Permissions.Contains(StaffPermissions.ForceclassSelf);

                case PlayerPermissions.ForceclassToSpectator:
                    return group.Permissions.Contains(StaffPermissions.ForceclassSpectator);

                case PlayerPermissions.ForceclassWithoutRestrictions:
                    return group.Permissions.Contains(StaffPermissions.ForceclassOthers);

                case PlayerPermissions.FriendlyFireDetectorImmunity:
                    return group.GroupFlags.Contains(StaffGroupFlags.IsFriendlyFireImmune);

                case PlayerPermissions.FriendlyFireDetectorTempDisable:
                    return group.Permissions.Contains(StaffPermissions.ServerConfigs);

                case PlayerPermissions.GameplayData:
                    return group.Permissions.Contains(StaffPermissions.GameplayData);

                case PlayerPermissions.GivingItems:
                    return group.Permissions.Contains(StaffPermissions.InventoryManagement);

                case PlayerPermissions.KickingAndShortTermBanning:
                    return group.Permissions.Contains(StaffPermissions.ShortBans);

                case PlayerPermissions.LongTermBanning:
                    return group.Permissions.Contains(StaffPermissions.LongBans);

                case PlayerPermissions.Overwatch:
                    return group.Permissions.Contains(StaffPermissions.PlayerManagement);

                case PlayerPermissions.PermissionsManagement:
                    return group.Permissions.Contains(StaffPermissions.ServerConfigs);

                case PlayerPermissions.ServerConsoleCommands:
                    return group.Permissions.Contains(StaffPermissions.ServerCommands);

                case PlayerPermissions.ViewHiddenBadges:
                    return group.GroupFlags.Contains(StaffGroupFlags.CanViewHiddenBadges);

                case PlayerPermissions.ViewHiddenGlobalBadges:
                    return group.GroupFlags.Contains(StaffGroupFlags.CanViewHiddenGlobalBadges);

                case PlayerPermissions.WarheadEvents:
                case PlayerPermissions.RespawnEvents:
                    return group.Permissions.Contains(StaffPermissions.MapManagement);

                case PlayerPermissions.RoundEvents:
                    return group.Permissions.Contains(StaffPermissions.RoundManagement);

                case PlayerPermissions.PlayerSensitiveDataAccess:
                    return group.Permissions.Contains(StaffPermissions.PlayerData);

                case PlayerPermissions.PlayersManagement:
                    return group.Permissions.Contains(StaffPermissions.PlayerManagement);

                default:
                    throw new Exception($"Unrecognized permissions node: {playerPermissions}");
            }
        }

        public static string GetColor(StaffColor color)
        {
            switch (color)
            {
                case StaffColor.ArmyGreen:
                    return "army_green";
                case StaffColor.BlueGreen:
                    return "blue_green";
                case StaffColor.DeepPink:
                    return "deep_pink";
                case StaffColor.LightGreen:
                    return "light_green";
                default:
                    return color.ToString().ToLowerInvariant();
            }
        }
    }
}
