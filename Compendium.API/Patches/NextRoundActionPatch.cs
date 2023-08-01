using Compendium.Colors;

using HarmonyLib;

namespace Compendium.Patches
{
    [HarmonyPatch(typeof(ServerStatic), nameof(ServerStatic.StopNextRound), MethodType.Setter)]
    public static class NextRoundActionPatch
    {
        public static bool Prefix(ServerStatic.NextRoundAction __value)
        {
            Hub.ForEach(hub =>
            {
                hub.Hint($"<b><color={ColorValues.LightGreen}>The server is going to restart <color={ColorValues.Red}>at the end of the round</color>!</color></b>", 5f, true);
            }, false);

            return true;
        }
    }
}