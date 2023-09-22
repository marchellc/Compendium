using helpers.Attributes;
using helpers.Verify;
using helpers.Time;
using helpers.Events;
using helpers.IO.Storage;
using helpers.Extensions;
using helpers;

using PluginAPI.Events;

using Compendium.Events;
using Compendium.TokenCache;
using Compendium.IdCache;
using Compendium.Round;
using Compendium.Activity;

using System.Collections.Generic;
using System.Linq;
using BetterCommands;
using System.Text;

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
            if (int.TryParse(query, out var pId) 
                && Hub.Hubs.TryGetFirst(h => h.PlayerId == pId, out var target))
            {
                record = GetData(target);
                return true;
            }

            return _records.Data.TryGetFirst(r =>
                    r != null
                 &&
                    (r.Id == query
                 || r.IpTracking.AllValues.Any(p => p.Value != null && p.Value == query)
                 || r.IdTracking.AllValues.Any(p => p.Value != null && (p.Value == query || p.Value.StartsWith(query)))
                 || (queryNick && r.NameTracking.AllValues.Any(p => p.Value != null && (p.Value.GetSimilarity(query) >= 0.8)))), out record);
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
            if (token is null)
                return null;

            if (!TryQuery(token.Ip, false, out var record) && !TryQuery(token.UserId, false, out record))
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

                _records.Add(data);
            }

            return data;
        }

        public static void UpdateData(ReferenceHub hub)
        {
            var data = GetData(hub);

            if (data is null)
                return;

            _activeRecords[hub] = data;

            data.IpTracking.Compare(hub.Ip());
            data.NameTracking.Compare(hub.Nick().Trim());
            data.IdTracking.Compare(hub.UserId());

            data.LastActivity = TimeUtils.LocalTime;

            _records.Save();

            OnRecordUpdated.Invoke(hub, data);
        }

        [Load]
        [Reload]
        public static void Load()
        {
            if (_records != null)
            {
                _records.Reload();
                return;
            }

            _records = new SingleFileStorage<PlayerDataRecord>($"{Directories.ThisData}/SavedPlayerData");
            _records.Load();
        }

        [Unload]
        public static void Unload()
        {
            if (_records != null)
                _records.Save();
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            UpdateData(ev.Player.ReferenceHub);
            ActivityRecorder.OnPlayerJoined(ev.Player.ReferenceHub, GetData(ev.Player.ReferenceHub));
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnRestart()
        {
            _tokenRecords.Clear();
            _activeRecords.Clear();
        }

        [Command("query", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Displays all available information about a record.")]
        private static string QueryCommand(ReferenceHub sender, PlayerDataRecord record)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"== Record ID: {record.Id} ==");
            sb.AppendLine($" > Tracked Names ({record.NameTracking.AllValues.Count}):");

            record.NameTracking.AllValues.For((i, pair) => sb.AppendLine($"   -> [{i + 1}] {pair.Value} ({pair.Key.ToString("F")})"));

            sb.AppendLine($" > Tracked Accounts ({record.IdTracking.AllValues.Count}):");

            record.IdTracking.AllValues.For((i, pair) => sb.AppendLine($"   -> [{i + 1}] {pair.Value} ({pair.Key.ToString("F")})"));

            sb.AppendLine($" > Tracked IPs");

            record.IpTracking.AllValues.For((i, pair) => sb.AppendLine($"   -> [{i + 1}] {pair.Value} ({pair.Key.ToString("F")})"));

            sb.AppendLine($" > Last Seen: {record.LastActivity.ToString("F")}");
            sb.AppendLine($" > Tracked Since: {record.CreationTime.ToString("F")}");

            return sb.ToString();
        }
    }
}
