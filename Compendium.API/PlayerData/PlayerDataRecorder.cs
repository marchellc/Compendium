using helpers.Attributes;
using helpers.Verify;
using helpers.Time;
using helpers.Events;
using helpers;

using PluginAPI.Events;

using BetterCommands;

using Compendium.Events;
using Compendium.TokenCache;
using Compendium.Generation;
using Compendium.Comparison;
using Compendium.Attributes;
using Compendium.Enums;
using Compendium.IO.Saving;

using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Compendium.PlayerData
{
    public static class PlayerDataRecorder
    {
        private static SaveFile<CollectionSaveData<PlayerDataRecord>> _records;

        private static Dictionary<ReferenceHub, TokenData> _tokenRecords = new Dictionary<ReferenceHub, TokenData>();
        private static Dictionary<ReferenceHub, PlayerDataRecord> _activeRecords = new Dictionary<ReferenceHub, PlayerDataRecord>();

        public static readonly EventProvider OnRecordUpdated = new EventProvider();

        public static bool TryQuery(string query, bool queryNick, out PlayerDataRecord record)
        {
            if (int.TryParse(query, out var plyId) 
                && Hub.Hubs.TryGetFirst(h => h.PlayerId == plyId, out var hub))
            {
                record = GetData(hub);
                return true;
            }

            var isIp = IPAddress.TryParse(query, out _);
            var isId = UserIdValue.TryParse(query, out var uid);

            foreach (var rec in _records.Data)
            {
                if (rec is null)
                    continue;

                if (rec.Id == query)
                {
                    record = rec;
                    return true;
                }

                if (rec.Ip == query && isIp)
                {
                    record = rec;
                    return true;
                }    

                if (isId && uid.Value == rec.UserId)
                {
                    record = rec;
                    return true;
                }

                if (!isId 
                    && !isIp 
                    && queryNick 
                    && NicknameComparison.Compare(query, rec.NameTracking.LastValue, 0.7))
                {
                    record = rec;
                    return true;
                }
            }

            record = null;
            return false;
        }

        public static TokenData GetToken(ReferenceHub hub)
        {
            if (!hub.IsPlayer() || !VerifyUtils.VerifyString(hub.authManager.GetAuthToken()))
                return null;

            if (_tokenRecords.TryGetValue(hub, out var tokenData))
                return tokenData;

            if (TokenParser.TryParse(hub.authManager.GetAuthToken(), out tokenData))
                return (_tokenRecords[hub] = tokenData);

            return null;
        }

        public static PlayerDataRecord GetData(TokenData token)
        {
            if (token is null)
                return null;

            if (!TryQuery(token.UserId, false, out var record))
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
                    Id = UniqueIdGeneration.Generate()
                };

                _records.Data.Add(data);
                _records.Save();
            }

            return data;
        }

        public static void UpdateData(ReferenceHub hub)
        {
            var data = GetData(hub);

            if (data is null)
                return;

            _activeRecords[hub] = data;

            data.Ip = hub.Ip();
            data.UserId = hub.UserId();
            data.NameTracking.Compare(hub.Nick().Trim());
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
                _records.Load();
                return;
            }

            _records = new SaveFile<CollectionSaveData<PlayerDataRecord>>(Directories.GetDataPath("SavedPlayerData", "playerData"));
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

            sb.AppendLine($" > Tracked Account: {record.UserId}");
            sb.AppendLine($" > Tracked IP: {record.Ip}");

            sb.AppendLine($" > Last Seen: {record.LastActivity.ToString("F")}");
            sb.AppendLine($" > Tracked Since: {record.CreationTime.ToString("F")}");

            return sb.ToString();
        }
    }
}