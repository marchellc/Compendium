using helpers.Patching;

namespace Compendium.Patches
{
    public static class NextRoundActionPatch
    {
        [Patch(typeof(ServerStatic), nameof(ServerStatic.StopNextRound), PatchType.Postfix, PatchMethodType.PropertySetter, "Server Action Announcement Patch")]
        public static void Postfix(ServerStatic.NextRoundAction value)
        {
            if (Plugin.Config.FeatureSettings.ServerActionAnnouncements.TryGetValue(value, out var announcement))
                World.Broadcast(announcement, 5);
        }
    }
}