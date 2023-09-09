using Compendium.Events;
using Compendium.PlayerData;
using Compendium.Round;

using helpers.Attributes;
using helpers.Extensions;
using helpers.IO.Storage;
using helpers.Time;

using PluginAPI.Events;
using PluginAPI.Helpers;

using System;
using System.Collections.Generic;

namespace Compendium.Activity
{
    public static class ActivityRecorder
    {
        private static Action<ReferenceHub, PlayerDataRecord> _onUpdated = new Action<ReferenceHub, PlayerDataRecord>(OnPlayerJoined);

        private static DateTime? _lastSave = null;

        private static SingleFileStorage<ActivityData> _records;
        private static Dictionary<ReferenceHub, ActivityData> _activeRecords = new Dictionary<ReferenceHub, ActivityData>();

        public static bool TryGetTotalPlaytime(string id, out TimeSpan playTime)
        {
            if (PlayerDataRecorder.TryQuery(id, false, out var record))
            {
                var acRecord = GetRecord(record);
                var seconds = 0;

                acRecord.Sessions.ForEach(session =>
                {
                    if (session.HasEnded)
                        seconds += session.Duration.Seconds;
                });

                playTime = TimeSpan.FromSeconds(seconds);
                return true;
            }

            playTime = default;
            return false;
        }

        public static bool TryGetTwoWeeksPlaytime(string id, out TimeSpan playTime)
        {
            if (PlayerDataRecorder.TryQuery(id, false, out var record))
            {
                var acRecord = GetRecord(record);
                var seconds = 0;

                acRecord.Sessions.ForEach(session =>
                {
                    if (session.HasEnded 
                        && session.IsBetween(TimeUtils.LocalTime.Subtract(TimeSpan.FromDays(14))))
                        seconds += session.Duration.Seconds;
                });

                playTime = TimeSpan.FromSeconds(seconds);
                return true;
            }

            playTime = default;
            return false;
        }

        public static ActivityData GetRecord(PlayerDataRecord playerDataRecord)
        {
            if (_records.Data.TryGetFirst(data => data.Id == playerDataRecord.Id, out var activityData))
                return activityData;

            activityData = new ActivityData { Id = playerDataRecord.Id };

            _records.Add(activityData);
            return activityData;
        }

        [Load]
        [Reload]
        private static void Load()
        {
            if (_records != null)
            {
                _records.Reload();
                return;
            }

            _records = new SingleFileStorage<ActivityData>($"{Directories.ThisData}/ActivityRecords");
            _records.Load();

            PlayerDataRecorder.OnRecordUpdated.Register(_onUpdated);

            Plugin.Info("Activity recording loaded.");
        }

        [Unload]
        private static void Unload()
        {
            if (_records != null)
                _records.Save();

            PlayerDataRecorder.OnRecordUpdated.Unregister(_onUpdated);

            Plugin.Info("Activity recording unloaded.");
        }

        public static void OnPlayerJoined(ReferenceHub hub, PlayerDataRecord record)
        {
            var acRecord = GetRecord(record);
            acRecord.BeginSession();
            _records.Save();
        }

        [Event]
        private static void OnPlayerLeft(PlayerLeftEvent ev)
        {
            var dataRecord = PlayerDataRecorder.GetData(ev.Player.ReferenceHub);
            var acRecord = GetRecord(dataRecord);

            acRecord.EndSession();
            _records.Save();
        }

        [UpdateEvent]
        private static void OnUpdate()
        {
            _records.Data.ForEach(record =>
            {
                record.GetCurrentSession().EndedAt = TimeUtils.LocalTime;
            });

            if (!_lastSave.HasValue || (TimeUtils.LocalTime - _lastSave.Value).TotalSeconds > 60)
            {
                _records.Save();
                _lastSave = TimeUtils.LocalTime;
            }
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnRestart()
        {
            _activeRecords.Clear();
            _lastSave = null;
        }
    }
}
