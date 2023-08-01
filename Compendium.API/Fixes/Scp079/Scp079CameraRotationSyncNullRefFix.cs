using Compendium.Round;

using helpers.Extensions;
using helpers.Patching;

using Mirror;

using PlayerRoles.PlayableScps.Scp079.Cameras;

using System.Collections.Generic;

namespace Compendium.Fixes
{
    public static class Scp079CameraRotationSyncNullRefFix
    {
        private static HashSet<Scp079CameraRotationSyncNullRefMethData> _methds = new HashSet<Scp079CameraRotationSyncNullRefMethData>();

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRoundEnd()
        {
            _methds.Clear();
        }

        [Patch(typeof(Scp079CameraRotationSync), nameof(Scp079CameraRotationSync.ServerProcessCmd))]
        public static bool Prefix(Scp079CameraRotationSync __instance, NetworkReader reader)
        {
            if (!_methds.TryGetFirst(s => s._netId == __instance._owner.netId, out var methData))
            {
                methData = new Scp079CameraRotationSyncNullRefMethData(__instance);
                _methds.Add(methData);
            }

            if ((__instance.CurrentCam != null && __instance.CurrentCam.SyncId != reader.ReadUShort()) || (__instance._lostSignalHandler != null && __instance._lostSignalHandler.Lost))
                return false;

            __instance.CurrentCam?.ApplyAxes(reader);

            methData._processSendRpcDel(true);
            return false;
        }
    }
}
