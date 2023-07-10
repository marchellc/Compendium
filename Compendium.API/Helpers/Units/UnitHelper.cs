using Respawning.NamingRules;
using Respawning;

using System.Collections.Generic;
using System.Linq;

using PlayerRoles;

using Mirror;

using PlayerRoles.FirstPersonControl;

using RelativePositioning;

namespace Compendium.Helpers.Units
{
    public static class UnitHelper
    {
        public const SpawnableTeamType Ntf = SpawnableTeamType.NineTailedFox;

        public static IReadOnlyList<string> NtfUnits
        {
            get
            {
                if (UnitNameMessageHandler.ReceivedNames.TryGetValue(Ntf, out var ntfUnits))
                    return ntfUnits;

                return null;
            }
        }

        public static bool TryCreateUnit(string unit)
        {
            if (UnitNameMessageHandler.ReceivedNames.TryGetValue(Ntf, out var units)
                && UnitNamingRule.AllNamingRules.TryGetValue(Ntf, out var rule))
            {
                if (!units.Contains(unit))
                {
                    units.Add(unit);

                    foreach (var hub in ReferenceHub.AllHubs)
                    {
                        if (hub.Mode != ClientInstanceMode.ReadyClient)
                            continue;

                        hub.connectionToClient.Send(new UnitNameMessage()
                        {
                            Team = Ntf,
                            NamingRule = rule,
                            UnitName = unit
                        });
                    }
                }

                return false;
            }

            return false;
        }

        public static bool TryGetUnitId(ReferenceHub hub, out byte unitId)
        {
            unitId = byte.MinValue;

            if (hub is null) 
                return false;

            if (hub.roleManager is null) 
                return false;

            if (hub.roleManager.CurrentRole is null) 
                return false;

            if (!(hub.roleManager.CurrentRole is HumanRole role) || role is null) 
                return false;

            if (!role.UsesUnitNames) 
                return false;

            unitId = role.UnitNameId;
            return true;
        }

        public static bool TryGetUnitName(ReferenceHub hub, out string unitName)
        {
            unitName = null;

            if (hub is null)
                return false;

            if (hub.roleManager is null)
                return false;

            if (hub.roleManager.CurrentRole is null)
                return false;

            if (!(hub.roleManager.CurrentRole is HumanRole role) || role is null)
                return false;

            if (!role.UsesUnitNames)
                return false;

            unitName = role.UnitName;
            return !string.IsNullOrWhiteSpace(unitName);
        }

        public static bool TrySetUnitId(ReferenceHub hub, byte unitId)
        {
            if (hub is null)
                return false;

            if (hub.roleManager is null)
                return false;

            if (hub.roleManager.CurrentRole is null)
                return false;

            if (!(hub.roleManager.CurrentRole is HumanRole role) || role is null)
                return false;

            if (!role.UsesUnitNames)
                return false;

            role.UnitNameId = unitId;

            SynchronizeUnitIdChange(hub, role);

            return true;
        }

        public static bool TrySetUnitName(ReferenceHub hub, string unitName, bool addIfMissing = false)
        {
            if (hub is null)
                return false;

            if (hub.roleManager is null)
                return false;

            if (hub.roleManager.CurrentRole is null)
                return false;

            if (!(hub.roleManager.CurrentRole is HumanRole role) || role is null)
                return false;

            if (!role.UsesUnitNames)
                return false;

            var units = NtfUnits?.ToList() ?? null;

            if (units is null) 
                return false;

            var unitIndex = units.IndexOf(unitName);

            if (unitIndex is -1)
            {
                if (!addIfMissing)
                    return false;

                if (!TryCreateUnit(unitName))
                    return false;

                units = NtfUnits.ToList();
                unitIndex = units.IndexOf(unitName);

                if (unitIndex is -1)
                    return false;

                return TrySetUnitId(hub, (byte)unitIndex);      
            }
            else
            {
                return TrySetUnitId(hub, (byte)unitIndex);
            }
        }

        public static bool TrySynchronizeUnits(ReferenceHub target, ReferenceHub source)
        {
            if (!TryGetUnitId(source, out var targetId))
                return false;

            return TrySetUnitId(target, targetId);
        }

        private static void SynchronizeUnitIdChange(ReferenceHub target, HumanRole role)
        {
            NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteUShort(38952);
            writer.WriteUInt(target.netId);
            writer.WriteRoleType(target.GetRoleId());
            writer.WriteByte(role.UnitNameId);

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
