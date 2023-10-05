using helpers.Patching;

namespace Compendium.Custom.Patches.Features
{
    public static class NextRoundActionPatch
    {
        [Patch(typeof(ServerStatic), nameof(ServerStatic.StopNextRound), PatchType.Postfix, PatchMethodType.PropertySetter, "Server Action Announcement Patch")]
        public static void Postfix(ServerStatic.NextRoundAction value)
        {
            if (Plugin.Config.ApiSetttings.ServerActionAnnouncements.TryGetValue(value, out var announcement))
                World.Broadcast(announcement, 5);
        }
    }
}