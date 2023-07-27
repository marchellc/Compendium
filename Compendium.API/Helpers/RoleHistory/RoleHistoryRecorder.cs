using Compendium.Helpers.Round;
using Compendium.Helpers.Snapshots;

using helpers;
using helpers.Attributes;
using helpers.Extensions;

using PlayerRoles;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Helpers.RoleHistory
{
    public static class RoleHistoryRecorder
    {
        private static Dictionary<string, List<RoleHistoryEntry>> _entries = new Dictionary<string, List<RoleHistoryEntry>>();

        public static bool TryGetPreviousRole(ReferenceHub hub, out RoleTypeId prevRole)
            => (prevRole = (TryGetHistory(hub, out var entries) && entries.Any() ? entries.Last().Previous : RoleTypeId.None)) != RoleTypeId.None;

        public static bool TryGetHistory(ReferenceHub hub, out List<RoleHistoryEntry> history)
            => _entries.TryGetValue(hub.UserId(), out history);

        [Load]
        private static void Initialize()
            => Reflection.TryAddHandler<PlayerRoleManager.ServerRoleSet>(typeof(PlayerRoleManager), "OnServerRoleSet", OnRoleChanged);

        [Unload]
        private static void Unload()
            => Reflection.TryRemoveHandler<PlayerRoleManager.ServerRoleSet>(typeof(PlayerRoleManager), "OnServerRoleSet", OnRoleChanged);

        private static void OnRoleChanged(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason reason)
        {
            if (_entries.TryGetValue(hub.UserId(), out var entries))
                entries.Add(new RoleHistoryEntry
                {
                    New = newRole,
                    Previous = hub.RoleId(),
                    Reason = reason,
                    Time = DateTime.Now.ToLocalTime(),
                    Inventory = hub.SaveInventory(),
                    Health = hub.Health()
                });
            else
                _entries.Add(hub.UserId(), new List<RoleHistoryEntry>()
                {
                    new RoleHistoryEntry
                    {
                        New = newRole,
                        Previous = hub.RoleId(),
                        Reason = reason,
                        Time = DateTime.Now.ToLocalTime(),
                        Inventory = hub.SaveInventory(),
                        Health = hub.Health()
                    }
                });
        }

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRoundRestart()
            => _entries.Clear();
    }
}