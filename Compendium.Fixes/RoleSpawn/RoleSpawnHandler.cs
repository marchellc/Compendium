using Compendium.Features;
using Compendium.Colors;

using helpers;
using helpers.Attributes;
using helpers.Pooling.Pools;

using Compendium.Round;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;

using UnityEngine;

using System.Linq;

namespace Compendium.Fixes.RoleSpawn
{
    public static class RoleSpawnHandler
    {
        public static readonly RoleTypeId[] ScpRoles = new RoleTypeId[6]
        {
            RoleTypeId.Scp049,
            RoleTypeId.Scp173,
            RoleTypeId.Scp106,
            RoleTypeId.Scp096,
            RoleTypeId.Scp079,
            RoleTypeId.Scp939
        };

        public static readonly RoleTypeId[] PossibleRoles = new RoleTypeId[]
        {
            RoleTypeId.Scientist,
            RoleTypeId.ClassD,
            RoleTypeId.FacilityGuard
        };

        [Load]
        public static void Load()
        {
            if (!Reflection.TryAddHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged))
                FLog.Warn($"Failed to register role spawn handler!");
            else
                FLog.Info($"Succesfully registered role spawn handler.");
        }

        [Unload]
        public static void Unload()
        {
            if (!Reflection.TryRemoveHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged))
                FLog.Warn($"Failed to remove role spawn handler!");
            else
                FLog.Info($"Succesfully removed role spawn handler.");
        }

        public static void FixPosition(ReferenceHub hub, Vector3 position, Vector3 rotation)
        {
            hub.TryOverridePosition(position, rotation);
        }

        [RoundStateChanged(RoundState.InProgress)]
        private static void OnRoundStarted()
        {
            Calls.Delay(1.5f, () =>
            {
                ScpRoles.ForEach(scpRole =>
                {
                    if (Hub.Hubs.Count(hub => hub.RoleId() == scpRole) > 1)
                    {
                        var plysToRemove = ListPool<ReferenceHub>.Pool.Get();

                        while (Hub.Hubs.Count(hub => hub.RoleId() == scpRole && !plysToRemove.Contains(hub)) > 1)
                            plysToRemove.Add(Hub.Hubs.Last(hub => hub.RoleId() == scpRole && !plysToRemove.Contains(hub)));

                        if (plysToRemove.Count > 0)
                        {
                            plysToRemove.ForEach(hub =>
                            {
                                FLog.Debug($"Removing duplicate SCP role from {hub.GetLogName(false, false)}: {scpRole}");

                                var newRole = PossibleRoles.RandomItem();

                                hub.RoleId(newRole);
                                hub.Hint($"\n\n<b><color={ColorValues.LightGreen}>Your role was set to <color={ColorValues.Red}>{newRole}</color> to prevent duplicate SCPs.</color></b>", 5f, true);
                            });
                        }

                        ListPool<ReferenceHub>.Pool.Push(plysToRemove);
                    }
                });
            });
        }

        private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
        {
            Calls.Delay(0.4f, () =>
            {
                hub.Health(hub.MaxHealth());

                if (newRole is null)
                    return;

                if (!newRole.Is<IFpcRole>())
                    return;

                if (!newRole.ServerSpawnFlags.HasFlag(RoleSpawnFlags.UseSpawnpoint))
                    return;

                var role = hub.GetRoleId();

                if (RoleSpawnValidator.IsEnabled(role, RoleSpawnValidationType.YAxis, out var axisValue))
                {
                    if (!RoleSpawnValidator.TryValidate(hub.transform.position, RoleSpawnValidationType.YAxis, axisValue))
                    {
                        if (hub is null || hub.roleManager is null || hub.roleManager.CurrentRole is null)
                            return;

                        if (hub.Mode != ClientInstanceMode.ReadyClient)
                            return;

                        var roleId = hub.GetRoleId();

                        if (roleId is RoleTypeId.Scp079 || roleId is RoleTypeId.Scp0492)
                            return;

                        if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
                            return;

                        if (fpcRole is null || fpcRole.SpawnpointHandler is null)
                            return;

                        if (!fpcRole.SpawnpointHandler.TryGetSpawnpoint(out var pos, out var rot))
                        {
                            FLog.Warn($"Failed to retrieve a spawnpoint of role {roleId}!");
                            return;
                        }

                        FixPosition(hub, pos, new Vector3(rot, rot, rot));
                        return;
                    }
                }

                if (RoleSpawnValidator.IsEnabled(role, RoleSpawnValidationType.SpawnpointDistance, out axisValue))
                {
                    if (hub is null || hub.roleManager is null || hub.roleManager.CurrentRole is null)
                        return;

                    if (hub.Mode != ClientInstanceMode.ReadyClient)
                        return;

                    var roleId = hub.GetRoleId();

                    if (roleId is RoleTypeId.Scp079 || roleId is RoleTypeId.Scp0492)
                        return;

                    if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
                        return;

                    if (fpcRole is null || fpcRole.SpawnpointHandler is null)
                        return;

                    if (!fpcRole.SpawnpointHandler.TryGetSpawnpoint(out var pos, out var rot))
                    {
                        FLog.Warn($"Failed to retrieve a spawnpoint of role {roleId}!");
                        return;
                    }

                    FixPosition(hub, pos, new Vector3(rot, rot, rot));
                }
            });
        }
    }
}