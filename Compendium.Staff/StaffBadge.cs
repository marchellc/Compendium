using System.Collections.Generic;

namespace Compendium.Staff
{
    public class StaffBadge
    {
        public string Name { get; set; } = "default";

        public StaffColor Color { get; set; } = StaffColor.Red;

        public List<StaffBadgeFlags> Flags { get; set; } = new List<StaffBadgeFlags>() { StaffBadgeFlags.IsCover, StaffBadgeFlags.IsHidden };


        public bool IsHidden()
            => Flags.Contains(StaffBadgeFlags.IsHidden);

        public bool IsCover()
            => Flags.Contains(StaffBadgeFlags.IsCover);

        public string GetColor()
        {
            switch (Color)
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
                    return Color.ToString().ToLowerInvariant();
            }
        }
    }
}
