using BetterCommands;
using BetterCommands.Permissions;

using Compendium.PlayerData;
using Compendium.IO.Saving;

using helpers;
using helpers.Attributes;
using helpers.Time;

using System;
using System.Linq;

using Compendium.Updating;
using Compendium.Attributes;

namespace Compendium.Staff
{
    public static class StaffActivity
    {
        internal static SaveFile<CollectionSaveData<StaffActivityData>> _storage;

        private static object _lock = new object();

        [Load]
        public static void Load()
        {
            if (_storage != null)
            {
                _storage.Load();
                return;
            }

            _storage = new SaveFile<CollectionSaveData<StaffActivityData>>(Directories.GetDataPath("SavedStaffPlaytime", "staffPlaytime"));

            Plugin.Info($"Loaded {_storage.Data.Count} activity record(s)");
        }

        public static void Reload()
        {
            if (_storage is null)
                Load();

            lock (_lock)
            {
                StaffHandler.Members.ForEach(p =>
                {
                    if (p.Value.Any(x => StaffHandler.Groups.TryGetValue(x, out var group) && group.GroupFlags.Contains(StaffGroupFlags.IsStaff))
                        && !_storage.Data.TryGetFirst(x => x.UserId == p.Key, out _))
                    {
                        _storage.Data.Add(new StaffActivityData()
                        {
                            Total = 0,
                            TwoWeeks = 0,
                            TwoWeeksStart = TimeUtils.LocalTime,
                            UserId = p.Key
                        });

                        if (Plugin.Config.ApiSetttings.ShowActivityDebug)
                            Plugin.Debug($"Added staff activity record for ID '{p.Key}'");
                    }
                });

                _storage.Save();
            }
        }

        [Update(Delay = 5000, PauseWaiting = false)]
        private static void OnUpdate()
        {
            if (_storage is null)
                return;

            lock (_lock)
            {
                for (int i = 0; i < _storage.Data.Count; i++)
                {
                    if (Hub.TryGetHub(_storage.Data[i].UserId, out var hub))
                    {
                        if (hub.RoleId() != PlayerRoles.RoleTypeId.Overwatch)
                        {
                            _storage.Data[i].Total += 5;
                            _storage.Data[i].TwoWeeks += 5;

                            if (Plugin.Config.ApiSetttings.ShowActivityDebug)
                                Plugin.Debug($"Recorded regular time for user ID '{_storage.Data[i].UserId}'");
                        }
                        else
                        {
                            _storage.Data[i].TotalOverwatch += 5;
                            _storage.Data[i].TwoWeeksOverwatch += 5;

                            if (Plugin.Config.ApiSetttings.ShowActivityDebug)
                                Plugin.Debug($"Record OW time for user ID '{_storage.Data[i].UserId}'");
                        }
                    }
                }
            }
        }

        [RoundStateChanged(Enums.RoundState.Restarting)]
        private static void OnRoundRestart()
            => _storage?.Save();

        [Command("resetactivity", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Resets the two-week activity counter for all staff members.")]
        private static string ResetActivityCommand(ReferenceHub sender)
        {
            lock (_lock)
            {
                _storage.Data.ForEach(x =>
                {
                    x.TwoWeeks = 0;
                    x.TwoWeeksOverwatch = 0;
                    x.TwoWeeksStart = TimeUtils.LocalTime;
                });

                _storage.Save();

                if (Plugin.Config.ApiSetttings.ShowActivityDebug)
                    Plugin.Debug($"Reset two-weeks activity by command.");
            }

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
                    sb.AppendLine($"{record.NameTracking.LastValue} ({record.UserId}): {TimeSpan.FromSeconds(x.TwoWeeks).UserFriendlySpan()} ({TimeSpan.FromSeconds(x.TwoWeeksOverwatch).UserFriendlySpan()} in OW) / {TimeSpan.FromSeconds(x.Total).UserFriendlySpan()} ({TimeSpan.FromSeconds(x.TotalOverwatch).UserFriendlySpan()} in OW) (two-weeks counter started at {x.TwoWeeksStart.ToString("G")})");
                else
                    sb.AppendLine($"{x.UserId}: {TimeSpan.FromSeconds(x.TwoWeeks).UserFriendlySpan()} ({TimeSpan.FromSeconds(x.TwoWeeksOverwatch).UserFriendlySpan()} in OW) / {TimeSpan.FromSeconds(x.Total).UserFriendlySpan()} ({TimeSpan.FromSeconds(x.TotalOverwatch).UserFriendlySpan()} in OW) (two-weeks counter started at {x.TwoWeeksStart.ToString("G")})");
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

            return $"{record.NameTracking.LastValue} ({record.UserId}): {TimeSpan.FromSeconds(data.TwoWeeks).UserFriendlySpan()} ({TimeSpan.FromSeconds(data.TwoWeeksOverwatch).UserFriendlySpan()} in OW) / {TimeSpan.FromSeconds(data.Total).UserFriendlySpan()} ({TimeSpan.FromSeconds(data.TotalOverwatch).UserFriendlySpan()} in OW) (two-weeks counter started at {data.TwoWeeksStart.ToString("G")})";
        }
    }
}
