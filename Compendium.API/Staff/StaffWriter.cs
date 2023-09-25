using Compendium.PlayerData;

using helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compendium.Staff
{
    public static class StaffWriter
    {
        internal static string MembersBuffer;
        internal static string GroupsBuffer;

        public static void WriteMembers(Dictionary<string, string[]> membersDict)
        {
            MembersBuffer = null;

            var sb = new StringBuilder();

            sb.AppendLine("# syntax: ID: groupKey1,groupKey2,groupKey3");
            sb.AppendLine("# example: 776561198456564: owner,developer");
            sb.AppendLine();

            foreach (var memberPair in membersDict)
            {
                if (PlayerDataRecorder.TryGetById(memberPair.Key, out var record))
                    sb.AppendLine($"# User: {record.NameTracking.LastValue} ({record.IpTracking.LastValue}; {record.Id})");

                sb.AppendLine($"{memberPair.Key}: {string.Join(",", memberPair.Value)}");
            }

            MembersBuffer = sb.ToString();
        }

        public static void WriteGroups(Dictionary<string, StaffGroup> groupsDict)
        {
            var permsDict = new Dictionary<StaffPermissions, List<string>>();
            var sb = new StringBuilder();

            sb.AppendLine("# group syntax: groupKey=text;color;kickPower;requiredKickPower;badgeFlags;groupFlags");
            sb.AppendLine($"# group colors: {string.Join(", ", Enum.GetValues(typeof(StaffColor)).Cast<StaffColor>().Select(c => c.ToString()))}");
            sb.AppendLine($"# group flags: {string.Join(", ", Enum.GetValues(typeof(StaffGroupFlags)).Cast<StaffGroupFlags>().Select(f => f.ToString()))}");
            sb.AppendLine($"# group badge flags: {string.Join(", ", Enum.GetValues(typeof(StaffBadgeFlags)).Cast<StaffBadgeFlags>().Select(f => f.ToString()))}");

            sb.AppendLine();

            sb.AppendLine("# permission syntax: permissionNode=groupKey1,groupKey2");
            sb.AppendLine($"# permission nodes: {string.Join(", ", Enum.GetValues(typeof(StaffPermissions)).Cast<StaffPermissions>().Select(p => p.ToString()))}");
            
            sb.AppendLine();

            sb.AppendLine($"# Groups");

            sb.AppendLine();

            groupsDict.ForEach(p =>
            {
                p.Value.Permissions.ForEach(perm =>
                {
                    if (!permsDict.ContainsKey(perm))
                        permsDict.Add(perm, new List<string>() { p.Key });
                    else
                        permsDict[perm].Add(p.Key);
                });

                sb.AppendLine($"{p.Key}={p.Value.Text};{p.Value.Color};{p.Value.KickPower};{p.Value.RequiredKickPower};{string.Join(",", p.Value.BadgeFlags.Select(f => f.ToString()))};{string.Join(",", p.Value.GroupFlags.Select(f => f.ToString()))}");
            });

            sb.AppendLine();
            sb.AppendLine($"# Permissions");
            sb.AppendLine();

            if (!permsDict.Any())
            {
                foreach (var perm in Enum.GetValues(typeof(StaffPermissions)).Cast<StaffPermissions>())
                    sb.AppendLine($"{perm}=");
            }
            else
            {
                permsDict.ForEach(pair => sb.AppendLine($"{pair.Key}={string.Join(",", pair.Value)}"));
            }

            GroupsBuffer = sb.ToString();
        }
    }
}