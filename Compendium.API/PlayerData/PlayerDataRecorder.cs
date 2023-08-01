using helpers.Attributes;
using helpers.Values;
using helpers.Verify;
using helpers.Time;
using helpers.Events;
using helpers.IO.Storage;
using helpers.Extensions;

using PluginAPI.Helpers;
using PluginAPI.Events;

using Compendium.Events;
using Compendium.TokenCache;
using Compendium.IdCache;
using Compendium.Round;

using System.Collections.Generic;
using System.Linq;

namespace Compendium.PlayerData
{
    public static class PlayerDataRecorder
    {
        private static SingleFileStorage<PlayerDataRecord> _records;

        private static Dictionary<ReferenceHub, TokenData> _tokenRecords = new Dictionary<ReferenceHub, TokenData>();
        private static Dictionary<ReferenceHub, PlayerDataRecord> _activeRecords = new Dictionary<ReferenceHub, PlayerDataRecord>();

        public static readonly EventProvider OnRecordUpdated = new EventProvider();

        public static bool TryQuery(string query, bool queryNick, out PlayerDataRecord record)
        {
            return _records.TryFirst(r =>
                    r.Id == query
                 || r.IpTracking.AllValues.Any(p => p.Value == query)
                 || r.IdTracking.AllValues.Any(p => p.Value == query || p.Value.StartsWith(query))
                 || (queryNick && r.NameTracking.AllValues.Any(p => p.Value.GetSimilarity(query) >= 0.8)), out record);
        }

        public static TokenData GetToken(ReferenceHub hub)
        {
            if (!hub.IsPlayer() || !VerifyUtils.VerifyString(hub.characterClassManager.AuthToken))
                return null;

            if (_tokenRecords.TryGetValue(hub, out var tokenData))
                return tokenData;

            if (TokenParser.TryParse(hub.characterClassManager.AuthToken, out tokenData))
                return (_tokenRecords[hub] = tokenData);

            return null;
        }

        public static PlayerDataRecord GetData(TokenData token)
        {
            if (!_records.TryFirst<PlayerDataRecord>(r => r.IpTracking.AllValues.Any(p => p.Value == token.Ip) || r.IdTracking.AllValues.Any(p => p.Value == token.UserId), out var record))
                return null;

            return record;
        }

        public static PlayerDataRecord GetData(ReferenceHub hub)
        {
            if (!_activeRecords.TryGetValue(hub, out var data))
            {

                var token = GetToken(hub);

                if (token is null)
                    return null;

                data = GetData(token);
            }

            if (data is null)
            {
                data = new PlayerDataRecord
                {
                    CreationTime = TimeUtils.LocalTime,
                    LastActivity = TimeUtils.LocalTime,
                    Id = IdGenerator.Generate()
                };

                _records.Append(data);
            }

            return data;
        }

        public static void UpdateData(ReferenceHub hub)
        {
            var data = GetData(hub);

            if (data is null)
                return;

            _activeRecords[hub] = data;

            data.IpTracking.Compare(Optional<string>.FromValue(hub.Ip()));
            data.NameTracking.Compare(Optional<string>.FromValue(hub.Nick().Trim()));
            data.IdTracking.Compare(Optional<string>.FromValue(hub.UserId()));
            data.LastActivity = TimeUtils.LocalTime;

            _records.Save();

            OnRecordUpdated.Invoke(hub, data);
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

            _records = new SingleFileStorage<PlayerDataRecord>($"{Paths.SecretLab}/player_data");
            _records.Load();
        }

        [Unload]
        private static void Unload()
        {
            if (_records != null)
                _records.Save();
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
            => UpdateData(ev.Player.ReferenceHub);

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnRestart()
        {
            _tokenRecords.Clear();
            _activeRecords.Clear();
        }
    }
}
