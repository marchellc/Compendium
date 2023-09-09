using Compendium.Colors;

using helpers.Configuration;

namespace Compendium.Gameplay.Broadcasts
{
    public static class LastBroadcastProperties
    {
        [Config(Name = "Last Show Room", Description = "Whether or not to show the room in the last broadcast.")]
        public static bool ShowRoom { get; set; }

        [Config(Name = "Last Show Zone", Description = "Whether or not to show the zone in the last broadcast.")]
        public static bool ShowZone { get; set; }

        [Config(Name = "Last Show Role", Description = "Whether or not to show the role in the last broadcast.")]
        public static bool ShowRole { get; set; }

        [Config(Name = "Last Show Name", Description = "Whether or not to show the name in the last broadcast.")]
        public static bool ShowName { get; set; }

        [Config(Name = "Last Broadcast Text", Description = "The text to broadcast. Possible variables: $room $zone $role $name")]
        public static string Text { get; set; } = $"<b><color={ColorValues.LightGreen}>Last player (<color={ColorValues.Red}>$name [$role]</color>) is located in <color={ColorValues.Red}>$room</color> (<color={ColorValues.Red}>$zone</color>)</color></b>";

        [Config(Name = "Last Broadcast Duration", Description = "The duration of the last player broadcast.")]
        public static int Duration { get; set; } = 7;
    }
}