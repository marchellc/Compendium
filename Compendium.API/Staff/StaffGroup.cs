using System.Collections.Generic;

namespace Compendium.Staff
{
    public class StaffGroup
    {
        public string Key { get; set; }
        public string Text { get; set; }

        public byte KickPower { get; set; }
        public byte RequiredKickPower { get; set; }

        public StaffColor Color { get; set; }

        public List<StaffPermissions> Permissions { get; }
        
        public IReadOnlyList<StaffBadgeFlags> BadgeFlags { get; set; }
        public IReadOnlyList<StaffGroupFlags> GroupFlags { get; set; }

        public StaffGroup(string key, string text, byte kickPower, byte requiredKickPower, StaffColor color, List<StaffBadgeFlags> badgeFlags, List<StaffGroupFlags> groupFlags)
        {
            Key = key;
            Text = text;
            Color = color;
            KickPower = kickPower;
            RequiredKickPower = requiredKickPower;

            Permissions = new List<StaffPermissions>();

            BadgeFlags = badgeFlags.AsReadOnly();
            GroupFlags = groupFlags.AsReadOnly();
        }
    }
}