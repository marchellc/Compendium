using BetterCommands;

using Compendium.Calls;
using Compendium.Round;
using Compendium.Snapshots;

using helpers;
using helpers.Attributes;
using helpers.Extensions;
using helpers.Time;

using PlayerRoles;

using PluginAPI.Core;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compendium.RoleHistory
{
    public static class RoleHistoryRecorder
    {
        private static Dictionary<string, List<RoleHistoryEntry>> _entries = new Dictionary<string, List<RoleHistoryEntry>>();

        public static bool TryGetPreviousRole(ReferenceHub hub, out RoleHistoryEntry prevRole)
        {
            if (_entries.TryGetValue(hub.UniqueId(), out var entries))
            {
                prevRole = entries.LastOrDefault();
                return prevRole != null && prevRole.Snapshot.Role.Role != RoleTypeId.None;
            }

            prevRole = null;
            return false;
        }

        public static bool TryGetHistory(ReferenceHub hub, out List<RoleHistoryEntry> history)
            => _entries.TryGetValue(hub.UniqueId(), out history);

        [Load]
        private static void Initialize()
            => Reflection.TryAddHandler<PlayerRoleManager.ServerRoleSet>(typeof(PlayerRoleManager), "OnServerRoleSet", OnRoleChanged);

        [Unload]
        private static void Unload()
            => Reflection.TryRemoveHandler<PlayerRoleManager.ServerRoleSet>(typeof(PlayerRoleManager), "OnServerRoleSet", OnRoleChanged);

        private static void OnRoleChanged(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason reason)
        {
            if (string.IsNullOrWhiteSpace(hub.UniqueId()) || !hub.IsPlayer() || newRole is RoleTypeId.None || !RoundHelper.IsStarted)
                return;

            CallHelper.CallWithDelay(() =>
            {
                if (_entries.TryGetValue(hub.UniqueId(), out var entries))
                    entries.Add(new RoleHistoryEntry
                    {
                        Reason = reason,
                        Snapshot = SnapshotHelper.Save(hub),
                        Time = TimeUtils.LocalTime
                    });
                else
                    _entries.Add(hub.UniqueId(), new List<RoleHistoryEntry>()
                    {
                        new RoleHistoryEntry
                        {
                            Reason = reason,
                            Snapshot = SnapshotHelper.Save(hub),
                            Time = TimeUtils.LocalTime
                        }
                    });

                Plugin.Debug($"Saved role history entry for {hub.GetLogName(true)}: {newRole} - {reason}");
            }, 3f);
        }

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRoundRestart()
            => _entries.Clear();


        [Command("rhistory.restore", CommandType.RemoteAdmin, CommandType.GameConsole)]
        private static string RestoryHistoryCommand(Player sender, Player target, uint number)
        {
            if (_entries.TryGetValue(target.ReferenceHub.UniqueId(), out var history))
            {
                if (number >= history.Count || number >= int.MaxValue)
                    return "History number is out of range.";

                var entry = history[(int)number];

                entry.Snapshot.Role.Apply(target.ReferenceHub);

                return $"Restored {target.Nickname} to entry from {entry.Time.ToString("F")}";
            }

            return $"No history records were found for {target.Nickname}.";
        }

        [Command("rhistory.list", CommandType.RemoteAdmin, CommandType.GameConsole)]
        private static string ListHistoryCommmand(Player sender, Player target)
        {
            if (_entries.TryGetValue(target.ReferenceHub.UniqueId(), out var history))
            {
                var sb = new StringBuilder();

                history.For((index, entry) =>
                {
                    sb.AppendLine($"《 HISTORY ENTRY {index + 1} 》");
                    sb.AppendLine($"➢ Changed at: {entry.Time.ToString("F")}")
                      .AppendLine($"➢ Role: {entry.Snapshot.Role.Role} ({entry.Snapshot.Role.Health} hp; {entry.Snapshot.Role.MaxHealth} max hp; {entry.Snapshot.Role.Stamina} stamina; {entry.Snapshot.Role.HumeShield} hume shield; position: {entry.Snapshot.Role.Position}")
                      .AppendLine($"➢ Nick: {entry.Snapshot.Nickname} ({entry.Snapshot.UserId})")
                      .AppendLine($"➢ Inventory: {string.Join(", ", entry.Snapshot.Role.Inventory.Inventory.Select(x => x.ToString().SpaceByPascalCase()))}")
                      .AppendLine($"➢ Ammo: {string.Join("| ", entry.Snapshot.Role.Inventory.Ammo.Select(p => $"{p.Key.ToString().SpaceByPascalCase()}: {p.Value}"))}")
                      .AppendLine();
                });

                return sb.ToString();
            }

            return $"No history recorded for {target.Nickname}";
        }
    }
}