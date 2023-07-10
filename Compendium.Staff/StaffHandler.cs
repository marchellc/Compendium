using Compendium.Features;
using Compendium.Helpers.Calls;
using Compendium.Helpers.Colors;
using Compendium.Helpers.Events;
using Compendium.Helpers.Overlay;
using Compendium.Helpers.UserId;

using helpers.Configuration.Ini;
using helpers.Extensions;
using helpers.Pooling.Pools;

using PluginAPI.Enums;
using PluginAPI.Events;
using PluginAPI.Helpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Compendium.Staff
{
    public static class StaffHandler
    {
        private static Action _reload;

        [IniConfig(Name = "Roles", Description = "A list of roles.")]
        public static List<StaffRole> Roles { get; set; } = new List<StaffRole>() { new StaffRole() };

        [IniConfig(Name = "Members", Description = "A list of staff members.")]
        public static Dictionary<string, string> MemberList { get; set; } = new Dictionary<string, string>() { ["default"] = "default" };

        [IniConfig(Name = "Alternative Command Permissions", Description = "A list of alternative permissions required for each command.")]
        public static Dictionary<string, StaffPermissions> AlternativeCommandPerms { get; set; } = new Dictionary<string, StaffPermissions>()
        {
            ["default"] = StaffPermissions.GameplayData
        };

        [IniConfig(Name = "Members Config Type", Description = "Location of the members config file.")]
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
                    roles._hub.ShowMessage($"\n\n<b><color={ColorValues.Red}>Revoked</color> server role</b>", 3f);

                roles.SetGroup(null, false, false, false);
                return;
            }

            if (ServerStatic.PermissionsHandler is null)
            {
                CallHelper.CallWhenFalse(() => SetGroup(roles, role), () => ServerStatic.PermissionsHandler is null);
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
            roles._hub.ShowMessage($"\n\n<b><color={ColorValues.Green}>Granted</color> server role</b>\n<b><color={ColorValues.LightGreen}>{role.Badge.Name}</color></b>", 3f);
        }

        public static void Initialize()
        {
            _reload = new Action(Reload);

            StaffFeature.Singleton.Config.OnReadFinished.Register(_reload);

            ServerEventType.PlayerJoined.AddHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);

            Members.Reload();
        }

        public static bool TryGetRole(string key, out StaffRole role)
        {
            foreach (var r in Roles)
            {
                if (r.Key == key)
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

            Members.Reload();
        }

        public static void Unload()
        {
            Members.Unload(MemberSaver);

            StaffFeature.Singleton.Config.OnReadFinished.Unregister(_reload);

            ServerEventType.PlayerJoined.RemoveHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);
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

                        CallHelper.CallWithDelay(() => RefreshRoles(), 1f);

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
                CallHelper.CallWithDelay(() => RefreshRoles(), 1f);
                return;
            }

            var lines = File.ReadAllLines(Path);
                
            if (!lines.Any())
            {
                FLog.Info($"No staff members were loaded - empty file.");
                CallHelper.CallWithDelay(() => RefreshRoles(), 1f);
                return;
            }

            var errorEncountered = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (errorEncountered)
                {
                    FLog.Warn($"An error was encountered - aborting load.");
                    CallHelper.CallWithDelay(() => RefreshRoles(), 1f);
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
                CallHelper.CallWithDelay(() => RefreshRoles(), 1f);
                return;
            }

            CallHelper.CallWithDelay(() => RefreshRoles(), 1f);
        }

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
    }
}