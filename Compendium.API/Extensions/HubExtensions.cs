using Mirror;

using PlayerRoles.FirstPersonControl;
using PlayerRoles;

using RelativePositioning;

using Respawning.NamingRules;

using System;

namespace Compendium.Extensions
{
    public static class HubExtensions
    {
        public static void SetUnit(this ReferenceHub hub, string name)
        {
            if (hub.roleManager.CurrentRole is HumanRole role)
            {
                if (UnitNameMessageHandler.ReceivedNames.TryGetValue(role.AssignedSpawnableTeam, out var units))
                {
                    var unitIndex = units.IndexOf(name);

                    if (unitIndex == -1)
                        throw new InvalidOperationException($"Unit of name {name} does not exist!");

                    hub.ChangeUnit((byte)unitIndex);
                }
            }
        }

        public static void ChangeUnit(this ReferenceHub target, byte newUnitId)
        {
            if (!(target.roleManager.CurrentRole is HumanRole role) || role is null || !role.UsesUnitNames)
                throw new InvalidOperationException($"Cannot change units of non-human roles.");

            role.UnitNameId = newUnitId;

            NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteUShort(38952);
            writer.WriteUInt(target.netId);
            writer.WriteRoleType(target.GetRoleId());
            writer.WriteByte(newUnitId);

            if (target.GetRoleId() != RoleTypeId.Spectator && target.roleManager.CurrentRole is IFpcRole fpc)
            {
                fpc.FpcModule.MouseLook.GetSyncValues(0, out ushort syncH, out _);

                writer.WriteRelativePosition(new RelativePosition(target.transform.position));
                writer.WriteUShort(syncH);
            }

            foreach (ReferenceHub targetHub in ReferenceHub.AllHubs)
            {
                if (targetHub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                targetHub.connectionToClient.Send(writer.ToArraySegment());
            }

            NetworkWriterPool.Return(writer);
        }
    }
}
