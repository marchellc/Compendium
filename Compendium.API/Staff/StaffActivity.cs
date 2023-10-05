using BetterCommands;
using BetterCommands.Permissions;

using Compendium.Events;
using Compendium.PlayerData;
using Compendium.Scheduling.Update;
using helpers;
using helpers.Attributes;
using helpers.IO.Storage;
using helpers.Time;

using System;
using System.Linq;

namespace Compendium.Staff
{
    public static class StaffActivity
    {
        internal static SingleFileStorage<StaffActivityData> _storage;
        private static DateTime _nextSave;

        [Load]
        public static void Load()
        {
            if (_storage != null)
            {
                _storage.Reload();
                return;
            }

            _storage = new SingleFileStorage<StaffActivityData>(Directories.GetDataPath("StaffPlaytime", "playtime"));
            _storage.Load();

            Plugin.Info($"Loaded {_storage.Data.Count} activity record(s)");
        }

        public static void Reload()
        {
            if (_storage is null)
                Load();

            StaffHandler.Members.ForEach(p =>
            {
                if (p.Value.Any(x => StaffHandler.Groups.TryGetValue(x, out var group) && group.GroupFlags.Contains(StaffGroupFlags.IsStaff))
                    && !_storage.TryFirst<StaffActivityData>(x => x.UserId == p.Key, out _))
                    _storage.Add(new StaffActivityData()
                    {
                        Total = 0,
                        TwoWeeks = 0,
                        TwoWeeksStart = TimeUtils.LocalTime,
                        UserId = p.Key
                    });
            });
        }

        [Update(Type = UpdateSchedulerType.SideThread, Delay = 5000)]
        private static void OnUpdate()
        {
            if (_storage is null)
                return;

            _storage.Data.ForEach(x =>
            {
                if (Hub.TryGetHub(x.UserId, out var player))
                {
                    x.Total += 5;
                    x.TwoWeeks += 5;
                }
            });

            if ((DateTime.Now - _nextSave).TotalMilliseconds > 5000)
            {
                _storage.Save();
                _nextSave = DateTime.Now;
            }
        }

        [Command("resetactivity", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Resets the two-week activity counter for all staff members.")]
        private static string ResetActivityCommand(ReferenceHub sender)
        {
            _storage.Data.ForEach(x =>
            {
                x.TwoWeeks = 0;
                x.TwoWeeksStart = TimeUtils.LocalTime;
            });

            _storage.Save();
            return "Reset activity for all staff members.";
        }

        [Command("totalactivity", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Shows a list of staff members and their total activity.")]
        private static string TotalActivityCommand(ReferenceHub sender)
        {
            var sb = Pools.PoolStringBuilder(true, $"Showing a list of {_storage.Data.Count} activity record(s)");
            var recs = _storage.Data.OrderByDescending(x => x.TwoWeeks);

            recs.ForEach(x =>
            {
                if (PlayerDataRecorder.TryQuery(x.UserId, false, out var record))
                    sb.AppendLine($"{record.NameTracking.LastValue} ({record.UserId}): {TimeSpan.FromSeconds(x.TwoWeeks).UserFriendlySpan()} / {TimeSpan.FromSeconds(x.Total).UserFriendlySpan()} (two-weeks counter started at {x.TwoWeeksStart.ToString("G")})");
                else
                    sb.AppendLine($"{x.UserId}: {TimeSpan.FromSeconds(x.TwoWeeks).UserFriendlySpan()} / {TimeSpan.FromSeconds(x.Total).UserFriendlySpan()} (two-weeks counter started at {x.TwoWeeksStart.ToString("G")}");
            });

            return sb.ReturnStringBuilderValue();
        }

        [Command("staffactivity", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Shows the total activity of a specified staff member.")]
        private static string StaffActivityCommand(ReferenceHub sender, string target)
        {
            StaffActivityData data = null;

            if (!PlayerDataRecorder.TryQuery(target, true, out var record)
                || !_storage.Data.TryGetFirst(x => x.UserId == record.UserId, out data)
                || data is null)
                return "Failed to find any activity records matching your query.";

            return $"{record.NameTracking.LastValue} ({record.UserId}): {TimeSpan.FromSeconds(data.TwoWeeks).UserFriendlySpan()} / {TimeSpan.FromSeconds(data.Total).UserFriendlySpan()} (two-weeks counter started at {data.TwoWeeksStart.ToString("G")}";
        }
    }
}
