using Compendium.Features;
using Compendium.Colors;
using Compendium.Events;
using Compendium.UserId;

using helpers.Configuration;
using helpers.Extensions;
using helpers.Pooling.Pools;

using PluginAPI.Events;
using PluginAPI.Helpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BetterCommands;
using BetterCommands.Permissions;

namespace Compendium.Staff
{
    public static class StaffHandler
    {
        private static Action _reload;

        [Config(Name = "Roles", Description = "A list of roles.")]
        public static List<StaffRole> Roles { get; set; } = new List<StaffRole>() { new StaffRole() };

        [Config(Name = "Members", Description = "A list of staff members.")]
        public static Dictionary<string, string> MemberList { get; set; } = new Dictionary<string, string>() { ["default"] = "default" };

        [Config(Name = "Alternative Command Permissions", Description = "A list of alternative permissions required for each command.")]
        public static Dictionary<string, StaffPermissions> AlternativeCommandPerms { get; set; } = new Dictionary<string, StaffPermissions>()
        {
            ["default"] = StaffPermissions.GameplayData
        };

        [Config(Name = "Members Config Type", Description = "Location of the members config file.")]
        public static StaffMembersConfigType MembersConfigLocation { get; set; } = StaffMembersConfigType.FeatureConfig;

        public static StaffMembersConfig Members { get; } = new StaffMembersConfig(MemberFiller);

        public static string Path
        {
            get
            {
                if (MembersConfigLocation is StaffMembersConfigType.ConfigFileGlobal)
                    return $"{Paths.AppData}/remote_admin_members.txt";
                else
                    return $"{Paths.Configs}/remote_admin_members.txt";
            }
        }

        public static void RefreshRoles()
        {
            FLog.Info($"Refreshing player roles ..");

            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (hub.serverRoles.RaEverywhere || hub.serverRoles.Staff)
                    continue;

                if (!Members.TryGetKey(hub.characterClassManager.UserId, out var roleKey) || !TryGetRole(roleKey, out var role))
                {
                    if (hub.serverRoles.Group != null)
                        SetGroup(hub.serverRoles, null);
                }
                else
                {
                    SetGroup(hub.serverRoles, role);
                }
            }
        }

        public static void SetGroup(ServerRoles roles, StaffRole role = null)
        {
            if (role is null)
            {
                if (roles.Group != null)
                    roles._hub.Hint($"\n\n<b><color={ColorValues.Red}>Revoked</color> server role</b>", 3f, true);

                roles.SetGroup(null, false, false, false);

                FLog.Debug($"Removed server role from {roles._hub.GetLogName(true, false)}");
                return;
            }

            if (ServerStatic.PermissionsHandler is null)
            {
                Calls.OnFalse(() => SetGroup(roles, role), () => ServerStatic.PermissionsHandler is null);
                return;
            }

            if (!ServerStatic.PermissionsHandler._groups.ContainsKey(role.Key))
                ServerStatic.PermissionsHandler._groups.Add(role.Key, new UserGroup()
                {
                    BadgeColor = role.Badge.GetColor(),
                    BadgeText = role.Badge.Name,
                    Cover = role.Badge.IsCover(),
                    HiddenByDefault = role.Badge.IsHidden(),
                    KickPower = role.KickPower.Power,
                    RequiredKickPower = role.KickPower.Required,
                    Shared = false,
                    Permissions = (ulong)StaffUtils.ToNwPermissions(role)
                });
            else
            {
                var group = ServerStatic.PermissionsHandler._groups[role.Key];

                group.Permissions = (ulong)StaffUtils.ToNwPermissions(role);
                group.BadgeText = role.Badge.Name;
                group.BadgeColor = role.Badge.GetColor();
                group.Cover = role.Badge.IsCover();
                group.HiddenByDefault = role.Badge.IsHidden();
                group.RequiredKickPower = role.KickPower.Required;
                group.KickPower = role.KickPower.Power;
                group.Shared = false;
            }

            ServerStatic.PermissionsHandler._members[roles._hub.characterClassManager.UserId] = role.Key;

            roles.RefreshPermissions();
            roles._hub.queryProcessor.GameplayData = role.HasPermission(StaffPermissions.GameplayData);
            roles._hub.Hint($"\n\n<b><color={ColorValues.Green}>Granted</color> server role</b>\n<b><color={ColorValues.LightGreen}>{role.Badge.Name}</color></b>", 3f, true);

            FLog.Debug($"Set server role of {roles._hub.LoggedNameFromRefHub()} to {role.Key} ({role.Badge.Name})");
        }

        public static void Initialize()
        {
            _reload = new Action(Reload);
            Members.Reload();
        }

        public static bool TryGetRole(string key, out StaffRole role)
        {
            foreach (var r in Roles)
            {
                if (string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    role = r;
                    return true;
                }
            }

            role = null;
            return false;
        }

        public static void Reload()
        {
            if (ServerStatic.PermissionsHandler != null)
            {
                ServerStatic.PermissionsHandler._members.Clear();
                ServerStatic.PermissionsHandler._groups.Clear();
            }

            if (!ValidateGroups())
            {
                FLog.Warn($"An error was detected in the role config! Aborting reload.");
                return;
            }

            Members.Reload();
        }

        public static bool ValidateGroups()
        {
            var anyChanged = false;

            foreach (var role in Roles)
            {
                if (Roles.Count(r => string.Equals(r.Key, role.Key, StringComparison.OrdinalIgnoreCase)) >= 2)
                {
                    FLog.Warn($"Detected duplicated role key: {role.Key}, removing duplicates ..");

                    while (Roles.Count(r => string.Equals(r.Key, role.Key, StringComparison.OrdinalIgnoreCase)) != 1)
                    {
                        Roles.Remove(Roles.First(r => r.Key == role.Key && r != role));
                    }
                }
            }

            return !anyChanged;
        }

        public static void Unload()
        {
            Members.Unload(MemberSaver);
        }

        private static void MemberSaver(Dictionary<string, string> members)
        {
            switch (MembersConfigLocation)
            {
                case StaffMembersConfigType.FeatureConfig:
                    {
                        MemberList.Clear();
                        MemberList.AddRange(members);

                        StaffFeature.Singleton.Config.Save();

                        return;
                    }

                case StaffMembersConfigType.ConfigFileGlobal:
                case StaffMembersConfigType.ConfigFileServer:
                    {
                        SaveConfig(members);
                        return;
                    }
            }
        }

        private static void MemberFiller(Dictionary<string, string> dict)
        {
            switch (MembersConfigLocation)
            {
                case StaffMembersConfigType.FeatureConfig:
                    {
                        dict.Clear();
                        dict.AddRange(MemberList);

                        Calls.Delay(1f, () => RefreshRoles());

                        return;
                    }

                case StaffMembersConfigType.ConfigFileGlobal:
                case StaffMembersConfigType.ConfigFileServer:
                    {
                        dict.Clear();

                        ReadConfig(dict);
                        return;
                    }
            }
        }

        private static void SaveConfig(Dictionary<string, string> members)
        {
            var lines = ListPool<string>.Pool.Get();

            for (int i = 0; i < members.Count; i++)
            {
                var member = members.ElementAt(i);

                lines.Add($"{member.Key}: {member.Value}");
            }

            File.WriteAllLines(Path, lines.ToArray());

            ListPool<string>.Pool.Push(lines);
        }

        private static void ReadConfig(Dictionary<string, string> members)
        {
            if (!File.Exists(Path))
            {
                File.Create(Path).Close();
                FLog.Info($"Generated default members file at {Path}");
                Calls.Delay(1f, () => RefreshRoles());
                return;
            }

            var lines = File.ReadAllLines(Path);
                
            if (!lines.Any())
            {
                FLog.Info($"No staff members were loaded - empty file.");
                Calls.Delay(1f, () => RefreshRoles());
                return;
            }

            var errorEncountered = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (errorEncountered)
                {
                    FLog.Warn($"An error was encountered - aborting load.");
                    continue;
                }

                var line = lines[i];

                if (line.StartsWith("#"))
                    continue;

                if (!line.TrySplit(':', true, 2, out var splits))
                {
                    FLog.Error($"Invalid line in the members file: failed to split \"{line}\"");
                    errorEncountered = true;
                    lines[i] = $"# ERROR - failed to split | {lines[i]}";
                    continue;
                }

                if (!UserIdHelper.TryParse(splits[0].Trim(), out var uid))
                {
                    FLog.Error($"Failed to parse {splits[0]} into a valid User ID!");
                    errorEncountered = true;
                    lines[i] = $"# ERROR - failed to parse User ID | {lines[i]}";
                    continue;
                }

                if (!TryGetRole(splits[1].Trim(), out var role))
                {
                    FLog.Error($"Unknown role: {splits[1]} (line: {line})");
                    errorEncountered = true;
                    lines[i] = $"# ERROR - unknown role | {lines[i]}";
                    continue;
                }

                members[uid.FullId] = role.Key;
            }

            if (errorEncountered)
            {
                File.WriteAllLines(Path, lines);
                Calls.Delay(1f, () => RefreshRoles());
                return;
            }

            Calls.Delay(1f, () => RefreshRoles());
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (Members.TryGetKey(ev.Player.UserId, out var roleKey))
            {
                if (TryGetRole(roleKey, out var role))
                {
                    SetGroup(ev.Player.ReferenceHub.serverRoles, role);
                }
            }
        }

        [Command("addgroup", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Adds a group.")]
        private static string AddGroupCommand(ReferenceHub sender, 
            
            string name, 
            string key,
            
            StaffColor color, 

            byte kickPower,
            byte requiredKickPower,
            
            List<StaffPermissions> permissions, 
            List<StaffBadgeFlags> badgeFlags, 
            List<StaffFlags> flags)
        {
            if (TryGetRole(key, out _))
                return "A role with that key already exists!";

            var group = new StaffRole
            {
                Badge = new StaffBadge
                {
                    Color = color,
                    Flags = badgeFlags,
                    Name = name
                },

                KickPower = new StaffKickPower
                {
                    Power = kickPower,
                    Required = requiredKickPower
                },

                Flags = flags,
                Key = key,
                Permissions = permissions
            };

            Roles.Add(group);
            StaffFeature.Singleton?.Config?.Save();

            return "Added a new group.";
        }

        [Command("delgroup", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Removes a group.")]
        private static string RemoveGroupCommand(ReferenceHub sender, string key)
        {
            if (!Roles.TryGetFirst(r => r.Key == key, out _))
                return "No roles with that key exist.";

            Roles.RemoveAll(r => r.Key == key);
            StaffFeature.Singleton?.Config?.Save();

            return "Role removed.";
        }

        [Command("delmember", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Removes a member.")]
        private static string RemoveMemberCommand(ReferenceHub sender, string memberId)
        {
            if (!UserIdHelper.TryParse(memberId, out var uid))
                return "Failed to parse User ID.";

            var members = Members.Members;

            if (!members.ContainsKey(uid.FullId) && !members.ContainsKey(uid.ClearId))
                return "That member does not have a role assigned.";

            members.Remove(uid.FullId);
            members.Remove(uid.ClearId);

            SaveConfig(members);

            return $"Removed role of {uid.FullId}";
        }

        [Command("setmember", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Sets a member's role.")]
        public static string SetMemberCommand(ReferenceHub sender, string memberId, string key)
        {
            if (!UserIdHelper.TryParse(memberId, out var uid))
                return "Failed to parse User ID.";

            if (!TryGetRole(key, out var role))
                return "Failed to find a role with that key.";

            Members.Members[uid.FullId] = role.Key;

            SaveConfig(MemberList);
            Reload();

            return $"Set role of {uid.FullId} to {role.Key} ({role.Badge.Name})";
        }

        [Command("listroles", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Lists all known roles.")]
        public static string ListRolesCommand(ReferenceHub sender)
        {
            if (!Roles.Any())
                return "There aren't any roles.";

            var sb = new StringBuilder();

            sb.AppendLine($"Showing a list of {Roles.Count} role(s):");

            Roles.For((i, role) =>
            {
                sb.AppendLine($"[{i}] {role.Key} '{role.Badge.Name}' ({role.Badge.Color}) flags='{string.Join(", ", role.Badge.Flags.Select(f => f.ToString()))}' badgeFlags='{string.Join(", ", role.Badge.Flags.Select(f => f.ToString()))}' permissions='{string.Join(", ", role.Permissions.Select(f => f.ToString()))}'");
            });

            return sb.ToString();
        }
    }
}