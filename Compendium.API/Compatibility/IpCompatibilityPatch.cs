using Compendium.TokenCache;

using HarmonyLib;

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
                && __instance.identity != null
                && ReferenceHub.TryGetHubNetID(__instance.identity.netId, out var hub)
                && TokenCacheHandler.TryRetrieve(hub, null, out var token))
            {
                __result = token.LastIp;
                return false;
            }

            return true;
        }
    }
}