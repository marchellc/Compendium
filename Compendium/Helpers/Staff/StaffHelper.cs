using Compendium.Attributes;
using Compendium.Helpers.Events;
using Compendium.Helpers.Hints;
using Compendium.Helpers.UserId;
using Compendium.State;

using helpers;
using helpers.Extensions;

using PluginAPI.Enums;
using PluginAPI.Events;
using PluginAPI.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Log = PluginAPI.Core.Log;

namespace Compendium.Helpers.Staff
{
    public static class StaffHelper
    {
        private static readonly Dictionary<UserIdValue, string> m_Staff = new Dictionary<UserIdValue, string>();
        private static FileSystemWatcher m_Watcher;

        public static IReadOnlyDictionary<UserIdValue, string> Staff => m_Staff;
        public static string FilePath => $"{Paths.Configs}/members.txt";

        [InitOnLoad(Priority = 200)]
        public static void Initialize()
        {
            ServerEventType.PlayerJoined.AddHandler<Action<PlayerJoinedEvent>>(OnConnected);
        }

        [InitOnLoad]
        public static void LoadStaff()
        {
            RemoveRoles();

            m_Staff.Clear();

            if (Plugin.Config.StaffSettings.FileWatcher)
            {
                if (m_Watcher is null)
                {
                    m_Watcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath), "*.txt");
                    m_Watcher.Changed += OnChange;

                    Log.Info($"File watcher started.", "Staff Helper");
                }    
            }
            else
            {
                if (m_Watcher != null)
                {
                    m_Watcher.Changed -= OnChange;
                    m_Watcher.Dispose();
                    m_Watcher = null;

                    Log.Info($"File watcher stopped.", "Staff Helper");
                }
            }

            Log.Debug($"Loading members from {FilePath} ..", Plugin.Config.StaffSettings.Debug, "Staff Helper");

            if (!File.Exists(FilePath))
            {
                File.Create(FilePath).Close();
                Log.Info($"No staff members were loaded - missing file.", "Staff Helper");
                return;
            }

            var lines = File.ReadAllLines(FilePath);
            if (!lines.Any())
            {
                Log.Info($"No staff members were loaded - empty file.", "Staff Helper");
                return;
            }

            foreach (var line in lines)
            {
                if (!line.TrySplit(':', true, 2, out var splits))
                {
                    Log.Warning($"Failed to split line ({line})!", "Staff Helper");
                    continue;
                }

                var userId = splits[0];
                var group = splits[1];

                if (!UserIdHelper.TryParse(userId, out var uid))
                {
                    Log.Warning($"Failed to parse User ID ({userId})!", "Staff Helper");
                    continue;
                }

                if (!TryGetGroup(group, out UserGroup _))
                {
                    Log.Warning($"Failed to find group {group}!", "Staff Helper");
                    continue;
                }

                m_Staff[uid] = group;
            }

            AssignRoles();           
        }

        public static void SaveStaff()
        {
            var sb = new StringBuilder();

            foreach (var pair in m_Staff)
            {
                var userId = pair.Key.ToString();
                var line = $"{userId}:{pair.Value}";

                sb.AppendLine(line);
            }

            File.WriteAllText(FilePath, sb.ToString());
        }

        public static void SetRole(ReferenceHub hub, string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return;

            ServerStatic.PermissionsHandler._members[hub.characterClassManager.UserId] = role;

            hub.serverRoles.RefreshPermissions();
            hub.characterClassManager.ConsolePrint($"Your server role has been set to: {role}", "green");

            Notify(hub, true);
        }

        public static void RemoveRole(ReferenceHub hub)
        {
            if (!TryGetGroup(hub.characterClassManager.UserId, out string _))
                return;

            ServerStatic.PermissionsHandler._members.Remove(hub.characterClassManager.UserId);

            hub.serverRoles.RefreshPermissions();
            hub.serverRoles.SetGroup(null, false);
            hub.characterClassManager.ConsolePrint($"Your server role has been revoked.", "red");

            Notify(hub, false);
        }

        public static bool TryGetGroup(string groupKey, out UserGroup group)
        {
            group = null;

            if (ServerStatic.PermissionsHandler is null)
            {
                Log.Warning($"Failed to find group {groupKey} as the server's permissions handler is null!", "Staff Helper");
                return false;
            }

            return ServerStatic.PermissionsHandler._groups.TryGetValue(groupKey, out group) && group != null;
        }

        public static bool TryGetGroup(string userId, out string groupKey)
        {
            if (m_Staff.Keys.Any(key => key.FullId == userId))
            {
                groupKey = m_Staff.First(pair => pair.Key.FullId == userId).Value;
                return true;
            }

            if (ServerStatic.PermissionsHandler != null)
            {
                if (ServerStatic.PermissionsHandler._members.TryGetValue(userId, out groupKey))
                {
                    return true;
                }
            }

            groupKey = null;
            return false;
        }

        public static bool IsConsideredStaff(ReferenceHub hub)
        {
            if (hub.Mode != ClientInstanceMode.ReadyClient)
                return false;

            if (hub.serverRoles.RaEverywhere && Plugin.Config.StaffSettings.GlobalModeratorsAreStaff)
                return true;

            if (hub.serverRoles.Staff && Plugin.Config.StaffSettings.NorthwoodStaffAreServer)
                return true;

            if (TryGetGroup(hub.characterClassManager.UserId, out string group))
            {
                if (Plugin.Config.StaffSettings.StaffRoles.Contains(group))
                {
                    return true;
                }
            }

            return false;
        }

        public static void RemoveRoles()
        {
            ReferenceHub.AllHubs.ForEach(hub =>
            {
                RemoveRole(hub);
            }, hub => hub.Mode == ClientInstanceMode.ReadyClient);
        }

        public static void AssignRoles()
        {
            ReferenceHub.AllHubs.ForEach(hub =>
            {
                if (TryGetGroup(hub.characterClassManager.UserId, out string group))
                {
                    SetRole(hub, group);
                }
            }, hub => hub.Mode == ClientInstanceMode.ReadyClient);
        }

        private static void Notify(ReferenceHub hub, bool added)
        {
            if (!Plugin.Config.StaffSettings.NotifyStaff)
                return;

            if (!IsConsideredStaff(hub))
                return;

            var hints = hub.GetOrAddState<HintController>();

            if (added)
            {
                hints.Display(new HintBuilder()
                    .WithFadeIn(0.8f)
                    .WithDuration(5f)
                    .Write(writer =>
                    {
                        writer.Clear();
                        writer.EmitSize(80);
                        writer.EmitTag(HintTag.Bold);
                        writer.EmitNewLine();
                        writer.EmitNewLine();
                        writer.EmitNewLine();
                        writer.EmitNewLine("Server role ");
                        writer.EmitColor("#33FF4F");
                        writer.Emit("assigned");
                        writer.EmitTagEnd(HintTag.Color);
                        writer.EmitTagEnd(HintTag.Bold);
                        writer.EmitNewLine($"「{hub.serverRoles.Network_myText.Trim()}」");
                        writer.EmitTagEnd(HintTag.Size);
                    })
                    .Build());
            }
            else
            {
                hints.Display(new HintBuilder()
                    .WithFadeIn(0.8f)
                    .WithDuration(5f)
                    .Write(writer =>
                    {
                        writer.Clear();
                        writer.EmitSize(80);
                        writer.EmitTag(HintTag.Bold);
                        writer.EmitNewLine();
                        writer.EmitNewLine();
                        writer.EmitNewLine();
                        writer.EmitNewLine("Server role ");
                        writer.EmitColor("#33FF4F");
                        writer.Emit("removed");
                        writer.EmitTagEnd(HintTag.Color);
                        writer.EmitTagEnd(HintTag.Bold);
                        writer.EmitTagEnd(HintTag.Size);
                    })
                    .Build());
            }
        }

        private static void OnConnected(PlayerJoinedEvent ev)
        {
            var player = ev.Player.ReferenceHub;

            if (TryGetGroup(player.characterClassManager.UserId, out string group))
            {
                SetRole(player, group);
            }
        }

        private static void OnChange(object sender, FileSystemEventArgs ev)
        {
            if (ev.ChangeType is WatcherChangeTypes.Renamed)
                return;

            Log.Debug($"Change detected in {ev.FullPath} ({ev.ChangeType})", Plugin.Config.StaffSettings.Debug, "Staff Helper");

            if (ev.FullPath != FilePath)
                return;

            LoadStaff();
        }
    }
}