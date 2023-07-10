using System.Collections.Generic;

namespace Compendium.Staff
{
    public class StaffRole
    {
        public string Key { get; set; } = "empty";

        public StaffBadge Badge { get; set; } = new StaffBadge();
        public StaffKickPower KickPower { get; set; } = new StaffKickPower();

        public List<StaffFlags> Flags { get; set; } = new List<StaffFlags>() { StaffFlags.IsStaff, StaffFlags.IsNoClip, StaffFlags.IsAdminChat, StaffFlags.IsAfkImmune, StaffFlags.IsFriendlyFireImmune };
        public List<StaffPermissions> Permissions { get; set; } = new List<StaffPermissions>() { StaffPermissions.Override };

        public List<string> CommandWhitelist { get; set; } = new List<string>() { "default" };
        public List<string> CommandBlacklist { get; set; } = new List<string>() { "default" };

        public bool IsAdminChat()
            => Flags.Contains(StaffFlags.IsAdminChat);

        public bool IsOverride()
            => Permissions.Contains(StaffPermissions.Override);

        public bool IsNoClip()
            => Flags.Contains(StaffFlags.IsNoClip);

        public bool IsAfkImmune()
            => Flags.Contains(StaffFlags.IsAfkImmune);

        public bool IsStaff()
            => Flags.Contains(StaffFlags.IsStaff);

        public bool IsFriendlyFireImmune()
            => Flags.Contains(StaffFlags.IsFriendlyFireImmune);

        public bool CanViewHiddenBadges(bool global)
            => Flags.Contains(global ? StaffFlags.CanViewHiddenGlobalBadges : StaffFlags.CanViewHiddenBadges);

        public bool IsAllowed(string cmd)
        {
            if (CommandBlacklist.Contains(cmd) || CommandBlacklist.Contains("*"))
                return false;

            if (CommandWhitelist.Count > 0)
            {
                if (CommandWhitelist.Count is 1 && CommandWhitelist[0] is "default")
                    return HasCommandPermission(cmd);
                else
                {
                    if (!CommandWhitelist.Contains(cmd))
                        return false;
                }
            }

            return HasCommandPermission(cmd);
        }

        public bool HasCommandPermission(string cmd)
        {
            if (StaffHandler.AlternativeCommandPerms.TryGetValue(cmd, out var perms))
                return HasPermission(perms);
            else
                return true;
        }

        public bool HasPermission(StaffPermissions staffPermissions)
            => Permissions.Contains(staffPermissions) || IsOverride();
    }
}