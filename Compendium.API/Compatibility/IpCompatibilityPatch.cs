using Compendium.PlayerData;

using helpers.Patching;

using Mirror;

namespace Compendium.Compatibility
{
    public static class IpCompatibilityPatch
    {
        [Patch(typeof(NetworkConnectionToClient), nameof(NetworkConnectionToClient.address), PatchType.Prefix, PatchMethodType.PropertyGetter)]
        public static bool AddressPatch(NetworkConnection __instance, ref string __result)
        {
            if (Plugin.Config.ApiSetttings.IpCompatibilityMode
                && Plugin.Config.ApiSetttings.IpCompatibilityModePatch
                && __instance.identity != null
                && ReferenceHub.TryGetHubNetID(__instance.identity.netId, out var hub))
            {
                __result = PlayerDataRecorder.GetToken(hub).Ip;
                return false;
            }

            return true;
        }
    }
}