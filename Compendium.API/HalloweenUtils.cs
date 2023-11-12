using Mirror;

using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.Ragdolls;
using PlayerRoles;
using PlayerStatsSystem;

using UnityEngine;

namespace Compendium
{
    public static class HalloweenUtils
    {
        public static void SpawnBones(Vector3 pos)
        {
            ReferenceHub.HostHub.roleManager.ServerSetRole(RoleTypeId.ClassD, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);

            if (ReferenceHub.HostHub.roleManager.CurrentRole is not HumanRole humanRole)
                return;

            humanRole.FpcModule.ServerOverridePosition(pos, Vector3.zero);

            var origRagdoll = RagdollManager.ServerSpawnRagdoll(ReferenceHub.HostHub, new UniversalDamageHandler(-1f, DeathTranslations.Warhead, null));
            var ragdoll = RagdollManager.ServerSpawnRagdoll(ReferenceHub.HostHub, new Scp3114DamageHandler(origRagdoll, false));

            NetworkServer.Destroy(origRagdoll.gameObject);

            if (ragdoll is null || ragdoll is not DynamicRagdoll dynamicRagdoll)
                return;

            ReferenceHub.HostHub.roleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);

            if (ReferenceHub.HostHub.roleManager.CurrentRole is not Scp3114Role scp3114Role)
                return;

            Scp3114RagdollToBonesConverter.ServerConvertNew(scp3114Role, dynamicRagdoll);

            ReferenceHub.HostHub.roleManager.ServerSetRole(RoleTypeId.None, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
        }
    }
}
