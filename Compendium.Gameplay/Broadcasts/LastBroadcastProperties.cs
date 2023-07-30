using Compendium.Colors;

using helpers.Configuration.Ini;

namespace Compendium.Gameplay.Broadcasts
{
    public static class LastBroadcastProperties
    {
        [IniConfig(Name = "Last Show Room", Description = "Whether or not to show the room in the last broadcast.")]
        public static bool ShowRoom { get; set; }

        [IniConfig(Name = "Last Show Zone", Description = "Whether or not to show the zone in the last broadcast.")]
        public static bool ShowZone { get; set; }

        [IniConfig(Name = "Last Show Role", Description = "Whether or not to show the role in the last broadcast.")]
        public static bool ShowRole { get; set; }

        [IniConfig(Name = "Last Show Name", Description = "Whether or not to show the name in the last broadcast.")]
        public static bool ShowName { get; set; }

        [IniConfig(Name = "Last Broadcast Text", Description = "The text to broadcast.\nPossible variables: $room $zone $role $name")]
        public static string Text { get; set; } = $"<b><color={ColorValues.LightGreen}>Last player (<color={ColorValues.Red}>$name [$role]</color>) is located in <color={ColorValues.Red}>$room</color> (<color={ColorValues.Red}>$zone</color>)</color></b>";

        [IniConfig(Name = "Last Broadcast Duration", Description = "The duration of the last player broadcast.")]
        public static int Duration { get; set; } = 7;
    }
}