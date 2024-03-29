﻿using Compendium.Attributes;
using Compendium.Enums;
using Compendium.Events;

using helpers;
using helpers.Attributes;
using helpers.IO.Watcher;
using helpers.Random;

using PluginAPI.Events;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Compendium.Staff
{
    public static class StaffHandler
    {
        private static bool _fwr;
        private static bool _saved;
        private static bool _firstSave;

        private static readonly Dictionary<string, string[]> _members = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, StaffGroup> _groups = new Dictionary<string, StaffGroup>();

        private static readonly Dictionary<string, UserGroup> _groupsById = new Dictionary<string, UserGroup>();
        private static readonly Dictionary<ReferenceHub, string> _usersById = new Dictionary<ReferenceHub, string>();

        public static string RolesFilePath => Directories.GetDataPath("Staff Groups.txt", "groups");
        public static string MembersFilePath => Directories.GetDataPath("Staff Members.txt", "members");

        public static IReadOnlyDictionary<string, string[]> Members => _members;
        public static IReadOnlyDictionary<string, StaffGroup> Groups => _groups;

        public static IReadOnlyDictionary<ReferenceHub, string> UserRoundIds => _usersById;
        public static IReadOnlyDictionary<string, UserGroup> GroupRoundIds => _groupsById;

        [Load]
        [Reload]
        private static void Load()
        {
            if (_saved)
                return;

            _members.Clear();
            _groups.Clear();

            if (!File.Exists(MembersFilePath))
                File.WriteAllText(MembersFilePath,
                    $"# syntax: ID: groupKey1,groupKey2,groupKey3\n" +
                    $"# example: 776561198456564: owner,developer");

            if (!File.Exists(RolesFilePath))
                File.WriteAllText(RolesFilePath,
                    $"# group syntax: groupKey=text;color;kickPower;requiredKickPower;badgeFlags;groupFlags\n" +
                    $"# group colors: {string.Join(", ", Enum.GetValues(typeof(StaffColor)).Cast<StaffColor>().Select(c => c.ToString()))}\n" +
                    $"# group flags: {string.Join(", ", Enum.GetValues(typeof(StaffGroupFlags)).Cast<StaffGroupFlags>().Select(f => f.ToString()))}\n" +
                    $"# group badge flags: {string.Join(", ", Enum.GetValues(typeof(StaffBadgeFlags)).Cast<StaffBadgeFlags>().Select(f => f.ToString()))}\n\n" +
                    $"# permission syntax: permissionNode=groupKey1,groupKey2\n" +
                    $"# permission nodes: {string.Join(", ", Enum.GetValues(typeof(StaffPermissions)).Cast<StaffPermissions>().Select(p => p.ToString()))}");

            StaffReader.GroupsBuffer = File.ReadAllLines(RolesFilePath);
            StaffReader.MembersBuffer = File.ReadAllLines(MembersFilePath);

            StaffReader.ReadMembers(_members);
            StaffReader.ReadGroups(_groups);

            ReassignGroups();

            StaffActivity.Reload();

            if (!_fwr)
            {
                StaticWatcher.AddHandler(RolesFilePath, Reflection.Method(typeof(StaffHandler), "Load"), null, NotifyFilters.LastWrite, WatcherChangeTypes.Changed);
                StaticWatcher.AddHandler(MembersFilePath, Reflection.Method(typeof(StaffHandler), "Load"), null, NotifyFilters.LastWrite, WatcherChangeTypes.Changed);

                _fwr = true;
            }

            Plugin.Info($"Loaded {_members.Count} member(s)");
            Plugin.Info($"Loaded {_groups.Count} group(s)");

            if (!_firstSave)
            {
                Save();
                _firstSave = true;
            }
        }

        private static void Save()
        {
            _saved = true;
            Calls.Delay(2f, () => _saved = false);

            StaffWriter.WriteGroups(_groups);
            StaffWriter.WriteMembers(_members);

            File.WriteAllText(MembersFilePath, StaffWriter.MembersBuffer);
            File.WriteAllText(RolesFilePath, StaffWriter.GroupsBuffer);

            StaffWriter.MembersBuffer = null;
            StaffWriter.GroupsBuffer = null;
        }

        [Unload]
        private static void Unload()
        {
            Save();
            ReassignGroups();
        }

        public static void ReassignGroups()
        {
            Hub.Hubs.ForEach(hub =>
            {
                if (hub.IsNorthwoodModerator() || hub.IsNorthwoodStaff())
                    return;

                SetRole(hub);
            });
        }

        public static void SetGroups(string userId, string[] groups)
        {
            _members[userId] = groups;
            Save();
        }

        public static void SetGroup(string userId, string group)
        {
            _members[userId] = new string[] { group };
            Save();
        }

        public static void RemoveGroup(string userId, string group)
        {
            if (!_members.TryGetValue(userId, out var groups))
                return;

            _members[userId] = groups.Where(g => g != group).ToArray();

            if (_members[userId].Length <= 0)
                _members.Remove(userId);

            Save();
        }

        public static void RemoveGroups(string userId, string[] groups)
        {
            if (!_members.TryGetValue(userId, out var curGroups))
                return;

            _members[userId] = curGroups.Where(g => !groups.Contains(g)).ToArray();

            if (_members[userId].Length <= 0)
                _members.Remove(userId);

            Save();
        }

        public static void AddGroup(string userId, string group)
        {
            if (!_groups.TryGetValue(group, out var groupValue))
                return;

            if (_members.TryGetValue(userId, out var groups))
            {
                if (group.Contains(groupValue.Key))
                    return;

                _members[userId] = groups.Concat(new string[] { groupValue.Key }).ToArray();

                Save();
            }
            else
            {
                _members[userId] = new string[] { groupValue.Key };
                Save();
            }
        }

        public static void RemoveMember(string userId)
        {
            if (_members.Remove(userId))
                Save();
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnWaiting()
        {
            _groupsById.Clear();
            _usersById.Clear();
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            Calls.Delay(0.2f, () => SetRole(ev.Player.ReferenceHub));
        }

        private static void SetRole(ReferenceHub target)
        {
            if (ServerStatic.PermissionsHandler is null)
            {
                Calls.OnFalse(() => SetRole(target), () => ServerStatic.PermissionsHandler is null);
                return;
            }

            if (_members.TryGetValue(target.UserId(), out var groupKeys)
                && groupKeys.Any())
            {
                if (!_usersById.TryGetValue(target, out var groupId) 
                    || !_groupsById.TryGetValue(groupId, out var ogGroup))
                    ogGroup = new UserGroup();

                ogGroup.Permissions = 0;
                ogGroup.BadgeText = string.Empty;
                ogGroup.BadgeColor = string.Empty;
                ogGroup.Cover = false;
                ogGroup.HiddenByDefault = false;
                ogGroup.RequiredKickPower = 0;
                ogGroup.KickPower = 0;
                ogGroup.Shared = false;

                groupKeys.ForEach(groupKey =>
                {
                    if (!_groups.TryGetValue(groupKey, out var group))
                    {
                        Plugin.Warn($"Failed to find group for key \"{groupKey}\"");
                        return;
                    }

                    if (group.GroupFlags.Contains(StaffGroupFlags.IsReservedSlot)
                        && !target.HasReservedSlot())
                        target.AddReservedSlot(false, true);

                    if (ogGroup.Permissions != 0)
                    {
                        var curPerms = (PlayerPermissions)ogGroup.Permissions;
                        var translatedPerms = StaffUtils.ToNwPermissions(group);

                        foreach (var perm in StaffUtils.Permissions)
                        {
                            if (PermissionsHandler.IsPermitted(ogGroup.Permissions, perm))
                                continue;

                            if (PermissionsHandler.IsPermitted((ulong)translatedPerms, perm))
                            {
                                curPerms |= perm;
                                ogGroup.Permissions = (ulong)curPerms;
                            }
                        }
                    }
                    else
                        ogGroup.Permissions = (ulong)StaffUtils.ToNwPermissions(group);

                    if (string.IsNullOrWhiteSpace(ogGroup.BadgeText))
                        ogGroup.BadgeText = group.Text;
                    else
                        ogGroup.BadgeText += $" | {group.Text}";

                    if (string.IsNullOrWhiteSpace(ogGroup.BadgeColor))
                        ogGroup.BadgeColor = StaffUtils.GetColor(group.Color);

                    if (!ogGroup.Cover && group.BadgeFlags.Contains(StaffBadgeFlags.IsCover))
                        ogGroup.Cover = true;

                    if (!ogGroup.HiddenByDefault && group.BadgeFlags.Contains(StaffBadgeFlags.IsHidden))
                        ogGroup.HiddenByDefault = true;

                    if (group.RequiredKickPower > ogGroup.RequiredKickPower)
                        ogGroup.RequiredKickPower = group.RequiredKickPower;

                    if (group.KickPower > ogGroup.KickPower)
                        ogGroup.KickPower = group.KickPower;
                });

                if (!_usersById.TryGetValue(target, out groupId))
                    groupId = _usersById[target] = RandomGeneration.Default.GetReadableString(30);

                _groupsById[groupId] = ogGroup;

                ServerStatic.PermissionsHandler._groups[groupId] = ogGroup;
                ServerStatic.PermissionsHandler._members[target.UserId()] = groupId;

                target.serverRoles.RefreshPermissions();
                target.queryProcessor.GameplayData = PermissionsHandler.IsPermitted(ogGroup.Permissions, PlayerPermissions.GameplayData);

                Plugin.Debug($"Set server role of {target.GetLogName(true)} to {ogGroup.BadgeText} (round ID: {groupId})");
            }
            else
            {
                if (ServerStatic.PermissionsHandler._members.TryGetValue(target.UserId(), out var groupId))
                {
                    ServerStatic.PermissionsHandler._groups.Remove(groupId);
                    ServerStatic.PermissionsHandler._members.Remove(groupId);

                    _groupsById.Remove(groupId);
                }

                _usersById.Remove(target);

                target.serverRoles.SetGroup(null, false, true);

                if (target.HasReservedSlot())
                    target.RemoveReservedSlot();
            }
        }
    }
}