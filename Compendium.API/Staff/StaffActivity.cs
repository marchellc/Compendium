using BetterCommands;
using BetterCommands.Permissions;

using Compendium.Events;
using Compendium.PlayerData;

using helpers;
using helpers.Attributes;
using helpers.IO.Binary;
using helpers.Time;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Staff
{
    public static class StaffActivity
    {
        private static Dictionary<string, StaffActivityData> _playtime;
        private static BinaryImage _playtimeFile;
        private static DateTime _nextSave;

        [Load]
        public static void Load()
        {
            if (_playtimeFile != null)
            {
                _playtimeFile.Load();

                if (!_playtimeFile.TryGetFirst(out _playtime))
                {
                    _playtimeFile.Add(_playtime = new Dictionary<string, StaffActivityData>());
                    _playtimeFile.Save();
                }
            }
            else
            {
                _playtimeFile = new BinaryImage(Directories.GetDataPath("StaffPlaytime", "playtime"));
                _playtimeFile.Load();

                if (!_playtimeFile.TryGetFirst(out _playtime))
                {
                    _playtimeFile.Add(_playtime = new Dictionary<string, StaffActivityData>());
                    _playtimeFile.Save();
                }
            }

            Plugin.Info($"Loaded {_playtime.Count} activity record(s)");
        }

        public static void Save()
        {
            _playtimeFile.Clear();
            _playtimeFile.Add(_playtime);
            _playtimeFile.Save();
        }

        public static void Reload()
        {
            if (_playtime is null || _playtimeFile is null)
                Load();
            else
                _playtime.Clear();

            StaffHandler.Members.ForEach(p =>
            {
                if (p.Value.Any(x => StaffHandler.Groups.TryGetValue(x, out var group) && group.GroupFlags.Contains(StaffGroupFlags.IsStaff))
                    && !_playtime.ContainsKey(p.Key))
                {
                    _playtime[p.Key] = new StaffActivityData();
                }
            });

            Save();
        }

        [UpdateEvent(TickRate = 5000)]
        private static void OnUpdate()
        {
            if (_playtime is null)
                return;

            if (_playtime.Count <= 0)
                return;

            _playtime.ForEach(x =>
            {
                if (Hub.TryGetHub(x.Key, out var player))
                {
                    x.Value.Total += 5;
                    x.Value.TwoWeeks += 5;
                }
            });

            if ((DateTime.Now - _nextSave).TotalMilliseconds > 30000)
            {
                Save();
                _nextSave = DateTime.Now;
            }
        }

        [Command("resetactivity", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Resets the two-week activity counter for all staff members.")]
        private static string ResetActivityCommand(ReferenceHub sender)
        {
            _playtime.ForEach(x =>
            {
                x.Value.TwoWeeks = 0;
                x.Value.TwoWeeksStart = TimeUtils.LocalTime;
            });

            Save();
            return "Reset activity for all staff members.";
        }

        [Command("totalactivity", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Shows a list of staff members and their total activity.")]
        private static string TotalActivityCommand(ReferenceHub sender)
        {
            var sb = Pools.PoolStringBuilder(true, $"Showing a list of {_playtime.Count} activity record(s)");

            _playtime.ForEach(x =>
            {
                if (PlayerDataRecorder.TryQuery(x.Key, false, out var record))
                    sb.AppendLine($"{record.NameTracking.LastValue} ({record.UserId}): {TimeSpan.FromSeconds(x.Value.TwoWeeks).UserFriendlySpan()} / {TimeSpan.FromSeconds(x.Value.Total).UserFriendlySpan()} (two-weeks counter started at {x.Value.TwoWeeksStart.ToString("G")}");
                else
                    sb.AppendLine($"(missing data record) {x.Key}: {TimeSpan.FromSeconds(x.Value.TwoWeeks).UserFriendlySpan()} / {TimeSpan.FromSeconds(x.Value.Total).UserFriendlySpan()} (two-weeks counter started at {x.Value.TwoWeeksStart.ToString("G")}");
            });

            return sb.ReturnStringBuilderValue();
        }

        [Command("staffactivity", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Shows the total activity of a specified staff member.")]
        private static string StaffActivityCommand(ReferenceHub sender, string target)
        {
            StaffActivityData data = null;

            if (!PlayerDataRecorder.TryQuery(target, true, out var record)
                || !_playtime.TryGetValue(record.UserId, out data)
                || data is null)
                return "Failed to find any activity records matching your query.";

            return $"{record.NameTracking.LastValue} ({record.UserId}): {TimeSpan.FromSeconds(data.TwoWeeks).UserFriendlySpan()} / {TimeSpan.FromSeconds(data.Total).UserFriendlySpan()} (two-weeks counter started at {data.TwoWeeksStart.ToString("G")}";
        }
    }
}
