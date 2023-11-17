using helpers;
using helpers.Configuration;
using helpers.Patching;
using helpers.Pooling.Pools;

using PlayerRoles;
using PlayerRoles.PlayableScps;
using PlayerRoles.RoleAssign;
using System.Linq;
using UnityEngine;

namespace Compendium.Gameplay.Spawning
{
    public static class SpawnHandler
    {
        [Config(Name = "SCP-3114 Spawn Chance", Description = "The chance of SCP-3114 spawning.")]
        public static float Scp3114Chance { get; set; } = 1f;

        public static bool EnableScp3114Spawn => Scp3114Chance > 0f;

        [Patch(typeof(ScpSpawner), nameof(ScpSpawner.NextScp), PatchType.Prefix, PatchMethodType.PropertyGetter)]
        private static bool GetNextScpPatch(ref RoleTypeId __result)
        {
            var chance = 0f;
            var count = ScpSpawner.SpawnableScps.Length;

            for (int i = 0; i < count; i++)
            {
                var role = ScpSpawner.SpawnableScps[i];

                if (ScpSpawner.EnqueuedScps.Contains(role.RoleTypeId))
                    ScpSpawner._chancesArray[i] = 0f;
                else if (role is ISpawnableScp spawnableScp)
                {
                    ScpSpawner._chancesArray[i] = Mathf.Max(spawnableScp.GetSpawnChance(ScpSpawner.EnqueuedScps), 0f);
                    chance += ScpSpawner._chancesArray[i];
                }
                else
                {
                    if (!EnableScp3114Spawn || role.RoleTypeId != RoleTypeId.Scp3114
                        || Hub.Count <= 2 || Hub.Hubs.Count(h => h.IsSCP()) <= 1)
                    {
                        ScpSpawner._chancesArray[i] = 0f;
                        continue;
                    }

                    ScpSpawner._chancesArray[i] = Scp3114Chance;
                }
            }

            if (chance == 0f)
            {
                __result = ScpSpawner.RandomLeastFrequentScp;
                return false;
            }

            var rnd = Random.Range(0f, count);

            for (int i = 0; i < count; i++)
            {
                rnd -= ScpSpawner._chancesArray[i];

                if (rnd < 0f)
                {
                    __result = ScpSpawner.SpawnableScps[i].RoleTypeId;
                    return false;
                }
            }

            __result = ScpSpawner.SpawnableScps[count - 1].RoleTypeId;
            return false;
        }

        [Patch(typeof(ScpSpawner), nameof(ScpSpawner.SpawnableScps), PatchType.Prefix, PatchMethodType.PropertyGetter)]
        private static bool GetSpawnableScpsPatch(ref PlayerRoleBase[] __result)
        {
            if (ScpSpawner._cacheSet)
            {
                __result = ScpSpawner._cachedSpawnableScps;
                return false;
            }

            var list = ListPool<PlayerRoleBase>.Pool.Get();

            foreach (var role in PlayerRoleLoader.AllRoles.Values)
            {
                if (role is null)
                    continue;

                if (role is not ISpawnableScp && (!EnableScp3114Spawn || role.RoleTypeId != RoleTypeId.Scp3114))
                    continue;

                list.Add(role);
            }

            ScpSpawner._chancesArray = new float[list.Count];
            ScpSpawner._cacheSet = true;
            ScpSpawner._cachedSpawnableScps = list.ToArray();

            list.ReturnList();
            list = null;

            __result = ScpSpawner._cachedSpawnableScps;
            return false;
        }
    }
}
