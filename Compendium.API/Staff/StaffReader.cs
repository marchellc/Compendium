using Compendium.UserId;

using helpers.Extensions;
using helpers;

using System;
using System.Collections.Generic;

namespace Compendium.Staff
{
    public static class StaffReader
    {
        internal static string[] MembersBuffer;
        internal static string[] GroupsBuffer;

        public static void ReadMembers(Dictionary<string, string[]> membersDict)
        {
            membersDict.Clear();

            if (MembersBuffer is null || MembersBuffer.IsEmpty())
                return;

            foreach (var line in MembersBuffer)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("#"))
                    continue;

                if (!line.TrySplit(':', true, 2, out var parts))
                {
                    Plugin.Warn($"Failed to parse line \"{line}\"!");
                    continue;
                }

                var id = parts[0].Trim();
                var group = parts[1].Trim();
                var groups = group.Split(',');

                if (!UserIdHelper.TryParse(id, out var idValue))
                {
                    Plugin.Warn($"Failed to parse ID \"{id}\"!");
                    continue;
                }

                for (int i = 0; i < groups.Length; i++)
                    groups[i] = groups[i].Trim();

                membersDict[idValue.FullId] = groups;
            }

            MembersBuffer = null;
        }

        public static void ReadGroups(Dictionary<string, StaffGroup> groupsDict)
        {
            groupsDict.Clear();

            if (GroupsBuffer is null || GroupsBuffer.IsEmpty())
                return;

            var permsDict = new Dictionary<StaffPermissions, List<string>>();

            foreach (var line in GroupsBuffer)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("#"))
                    continue;

                if (line.TrySplit('=', true, 2, out var parts))
                {
                    var key = parts[0];

                    if (parts[1].TrySplit(';', true, 6, out var groupParts))
                    {
                        var text = groupParts[0].Trim();
                        var colorStr = groupParts[1].Trim();
                        var kickPowerStr = groupParts[2].Trim();
                        var requiredKickPowerStr = groupParts[3].Trim();
                        var badgeFlagsStr = groupParts[4].Trim();
                        var groupFlagsStr = groupParts[5].Trim();

                        if (!byte.TryParse(kickPowerStr, out var kickPower))
                        {
                            Plugin.Warn($"Failed to parse the kick power of \"{key}\"");
                            continue;
                        }

                        if (!byte.TryParse(requiredKickPowerStr, out var requiredKickPower))
                        {
                            Plugin.Warn($"Failed to parse the required kick power of \"{key}\"");
                            continue;
                        }

                        if (!Enum.TryParse<StaffColor>(colorStr, true, out var color))
                        {
                            Plugin.Warn($"Failed to parse the color of \"{key}\"");
                            continue;
                        }

                        if (!badgeFlagsStr.TrySplit(',', true, null, out var badgeFlagsCol))
                        {
                            Plugin.Warn($"Failed to parse a list of badge flags of role \"{key}\"");
                            continue;
                        }

                        if (!groupFlagsStr.TrySplit(',', true, null, out var groupFlagsCol))
                        {
                            Plugin.Warn($"Failed to parse a list of group flags of role \"{key}\"");
                            continue;
                        }

                        var badgeFlags = new List<StaffBadgeFlags>();
                        var groupFlags = new List<StaffGroupFlags>();

                        badgeFlagsCol.ForEach(str =>
                        {
                            if (!Enum.TryParse<StaffBadgeFlags>(str.Trim(), true, out var badgeFlag))
                            {
                                Plugin.Warn($"Failed to parse badge flag \"{str}\" of role \"{key}\"");
                                return;
                            }

                            badgeFlags.Add(badgeFlag);
                        });

                        groupFlagsCol.ForEach(str =>
                        {
                            if (!Enum.TryParse<StaffGroupFlags>(str.Trim(), true, out var groupFlag))
                            {
                                Plugin.Warn($"Failed to parse group flag \"{str}\" of role \"{key}\"");
                                return;
                            }

                            groupFlags.Add(groupFlag);
                        });

                        groupsDict[key] = new StaffGroup(key, text, kickPower, requiredKickPower, color, badgeFlags, groupFlags);
                    }
                    else if (parts[1].TrySplit(',', true, null, out var permParts))
                    {
                        if (!Enum.TryParse<StaffPermissions>(key.Trim(), true, out var permFlag))
                        {
                            Plugin.Warn($"Failed to parse permission flag \"{key}\"");
                            continue;
                        }

                        permsDict[permFlag] = new List<string>();
                        permParts.ForEach(str =>
                        {
                            if (!groupsDict.TryGetValue(str.Trim(), out var group))
                            {
                                Plugin.Warn($"Failed to find group for permission flag \"{permFlag}\": \"{str}\"");
                                return;
                            }

                            permsDict[permFlag].Add(group.Key);
                        });
                    }
                    else
                        Plugin.Warn($"Found an unknown line: \"{line}\"");
                }
                else
                    Plugin.Warn($"Found an unknown line: \"{line}\"");
            }

            permsDict.ForEach(p =>
            {
                p.Value.ForEach(k =>
                {
                    if (groupsDict.TryGetValue(k, out var group))
                    {
                        group.Permissions.Add(p.Key);
                    }
                });
            });

            GroupsBuffer = null;
        }
    }
}