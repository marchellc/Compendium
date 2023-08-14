using HarmonyLib;

namespace Compendium.Patches
{
    [HarmonyPatch(typeof(ServerStatic), nameof(ServerStatic.StopNextRound), MethodType.Setter)]
    public static class NextRoundActionPatch
    {
        public static void Postfix(ServerStatic.NextRoundAction __value)
        {
            if (Plugin.Config.FeatureSettings.ServerActionAnnouncements.TryGetValue(__value, out var announcement))
                World.Broadcast(announcement, 5);
        }
    }
}