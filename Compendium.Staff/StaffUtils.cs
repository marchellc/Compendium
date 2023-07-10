using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Staff
{
    public static class StaffUtils
    {
        public static IReadOnlyList<PlayerPermissions> Permissions => Enum.GetValues(typeof(PlayerPermissions))
            .Cast<PlayerPermissions>()
            .ToList();

        public static PlayerPermissions ToNwPermissions(StaffRole role)
        {
            var list = Permissions;

            PlayerPermissions perms = default;

            foreach (var perm in list)
            {
                if (HasPermission(role, perm))
                {
                    perms |= perm;
                }
            }

            return perms;
        }

        public static bool HasPermission(StaffRole role, PlayerPermissions playerPermissions)
        {
            if (role.IsOverride())
                return true;

            switch (playerPermissions)
            {
                case PlayerPermissions.Noclip:
                    return role.IsNoClip();

                case PlayerPermissions.BanningUpToDay:
                    return role.HasPermission(StaffPermissions.DayBans);

                case PlayerPermissions.AFKImmunity:
                    return role.IsAfkImmune();

                case PlayerPermissions.SetGroup:
                    return role.HasPermission(StaffPermissions.ServerConfigs);

                case PlayerPermissions.AdminChat:
                    return role.IsAdminChat();

                case PlayerPermissions.ServerConfigs:
                    return role.HasPermission(StaffPermissions.ServerConfigs);

                case PlayerPermissions.Announcer:
                    return role.HasPermission(StaffPermissions.CassieAccess);

                case PlayerPermissions.Broadcasting:
                    return role.HasPermission(StaffPermissions.BroadcastAccess);

                case PlayerPermissions.Effects:
                    return role.HasPermission(StaffPermissions.PlayerManagement);

                case PlayerPermissions.FacilityManagement:
                    return role.HasPermission(StaffPermissions.MapManagement);

                case PlayerPermissions.ForceclassSelf:
                    return role.HasPermission(StaffPermissions.ForceclassSelf);

                case PlayerPermissions.ForceclassToSpectator:
                    return role.HasPermission(StaffPermissions.ForceclassSpectator);

                case PlayerPermissions.ForceclassWithoutRestrictions:
                    return role.HasPermission(StaffPermissions.ForceclassOthers);

                case PlayerPermissions.FriendlyFireDetectorImmunity:
                    return role.IsFriendlyFireImmune();

                case PlayerPermissions.FriendlyFireDetectorTempDisable:
                    return role.HasPermission(StaffPermissions.ServerConfigs);

                case PlayerPermissions.GameplayData:
                    return role.HasPermission(StaffPermissions.GameplayData);

                case PlayerPermissions.GivingItems:
                    return role.HasPermission(StaffPermissions.InventoryManagement);

                case PlayerPermissions.KickingAndShortTermBanning:
                    return role.HasPermission(StaffPermissions.ShortBans);

                case PlayerPermissions.LongTermBanning:
                    return role.HasPermission(StaffPermissions.LongBans);

                case PlayerPermissions.Overwatch:
                    return role.HasPermission(StaffPermissions.PlayerManagement);

                case PlayerPermissions.PermissionsManagement:
                    return role.HasPermission(StaffPermissions.ServerConfigs);

                case PlayerPermissions.ServerConsoleCommands:
                    return role.HasPermission(StaffPermissions.ServerCommands);

                case PlayerPermissions.ViewHiddenBadges:
                    return role.CanViewHiddenBadges(false);

                case PlayerPermissions.ViewHiddenGlobalBadges:
                    return role.CanViewHiddenBadges(true);

                case PlayerPermissions.WarheadEvents:
                case PlayerPermissions.RespawnEvents:
                    return role.HasPermission(StaffPermissions.MapManagement);

                case PlayerPermissions.RoundEvents:
                    return role.HasPermission(StaffPermissions.RoundManagement);

                case PlayerPermissions.PlayerSensitiveDataAccess:
                    return role.HasPermission(StaffPermissions.PlayerData);

                case PlayerPermissions.PlayersManagement:
                    return role.HasPermission(StaffPermissions.PlayerManagement);

                default:
                    throw new Exception($"Unrecognized permissions node: {playerPermissions}");
            }
        }
    }
}