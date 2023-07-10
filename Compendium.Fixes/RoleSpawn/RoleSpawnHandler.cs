using Compendium.Extensions;
using Compendium.Features;
using Compendium.Helpers.Calls;
using Compendium.Helpers.Overlay;

using helpers;
using helpers.Extensions;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;

using UnityEngine;

namespace Compendium.Fixes.RoleSpawn
{
    public static class RoleSpawnHandler
    {
        public static void Load()
        {
            if (!Reflection.TryAddHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged))
                FLog.Warn($"Failed to register role spawn handler!");
            else
                FLog.Info($"Succesfully registered role spawn handler.");
        }

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

        private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
        {
            CallHelper.CallWithDelay(() =>
            {
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
            }, 0.4f);
        }
    }
}