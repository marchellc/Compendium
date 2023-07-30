using BetterCommands;
using BetterCommands.Permissions;

using Compendium.Calls;
using Compendium.Events;
using Compendium.Round;

using Compendium.IdCache;

using helpers.Attributes;
using helpers.Extensions;
using helpers.IO.Storage;

using PluginAPI.Core;
using PluginAPI.Events;
using PluginAPI.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.TokenCache
{
    public static class TokenCacheHandler
    {
        private static object _lockObj = new object();

        private static SingleFileStorage<TokenCacheData> _tokenStorage;
        private static Dictionary<ReferenceHub, TokenData> _tokens = new Dictionary<ReferenceHub, TokenData>();

        public static bool TryGetToken(ReferenceHub hub, out TokenData tokenData)
        {
            if (_tokens.TryGetValue(hub, out tokenData))
                return true;

            if (!TokenParser.TryParse(hub.characterClassManager.AuthToken, out tokenData))
                return false;

            _tokens[hub] = tokenData;
            return true;
        }

        public static bool TryRetrieveByUserId(string userId, out TokenCacheData tokenCacheData)
        {
            if (_tokenStorage.TryFirst(cache => cache.Ids.Any(val => val.Value == userId) || cache.UniqueId == userId, out tokenCacheData))
                return true;

            tokenCacheData = null;
            return false;
        }

        public static bool TryRetrieveByIp(string ip, out TokenCacheData tokenCacheData)
        {
            if (_tokenStorage.TryFirst(cache => cache.Ips.Any(val => val.Value == ip), out tokenCacheData))
                return true;

            tokenCacheData = null;
            return false;
        }

        public static bool TryRetrieveByNickname(string nick, double minScore, out TokenCacheData tokenCacheData)
        {
            var possible = _tokenStorage.GetAll<TokenCacheData>().Where(cache => cache.Nicknames.Any(n => n.Value.GetSimilarity(nick) >= minScore));

            if (!possible.Any())
            {
                tokenCacheData = null;
                return false;
            }

            var sorted = possible.OrderByDescending(cache => cache.Nicknames.OrderByDescending(n => n.Value.GetSimilarity(nick)).First().Value.GetSimilarity(nick));

            tokenCacheData = sorted.FirstOrDefault();
            return tokenCacheData != null;
        }

        public static bool TryRetrieve(ReferenceHub hub, TokenData tokenData, out TokenCacheData tokenCacheData)
        {
            if (tokenData is null)
            {
                if (!TryGetToken(hub, out tokenData))
                {
                    tokenCacheData = null;
                    return false;
                }
            }

            if (!_tokenStorage.TryFirst(cache => cache.EhId == tokenData.EhId || cache.Public == tokenData.PublicPart || cache.Signature == tokenData.Signature, out tokenCacheData))
            {
                tokenCacheData = null;
                return false;
            }

            return tokenCacheData != null;
        }

        public static bool TryRetrieveOrAdd(ReferenceHub hub, ref TokenData tokenData, out TokenCacheData tokenCacheData)
        {
            if (tokenData is null)
            {
                if (!TryGetToken(hub, out tokenData))
                {
                    tokenCacheData = null;
                    return false;
                }
            }    

            if (!TryRetrieve(hub, tokenData, out tokenCacheData))
            {
                tokenCacheData = new TokenCacheData();

                tokenCacheData.RecordIdChange(hub.UserId());
                tokenCacheData.RecordNicknameChange(hub.Nick());
                tokenCacheData.RecordSerial(tokenData.SerialNumber);
                tokenCacheData.RecordIpChange(hub.Ip());

                tokenCacheData.EhId = tokenData.EhId;
                tokenCacheData.UniqueId = IdGenerator.Generate();
                tokenCacheData.Signature = tokenData.Signature;
                tokenCacheData.Public = tokenData.PublicPart;

                _tokenStorage.Add(tokenCacheData);

                return true;
            }
            else
            {
                return tokenCacheData != null;
            }
        }

        [Load]
        private static void Initialize()
        {
            _tokenStorage = new SingleFileStorage<TokenCacheData>($"{Paths.SecretLab}/token_storage");
            _tokenStorage.Load();
        }

        [Unload]
        private static void Unload()
        {
            _tokenStorage.Save();
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (!ev.Player.ReferenceHub.IsPlayer())
                return;

            if (string.IsNullOrWhiteSpace(ev.Player.ReferenceHub.characterClassManager.AuthToken))
                CallHelper.CallWhenFalse(() => OnJoined(ev.Player.ReferenceHub), () => string.IsNullOrWhiteSpace(ev.Player.ReferenceHub.characterClassManager.AuthToken));
            else
                OnJoined(ev.Player.ReferenceHub);
        }

        private static void OnJoined(ReferenceHub hub)
        {
            lock (_lockObj)
            {
                TryGetToken(hub, out var tokenData);
                TryRetrieveOrAdd(hub, ref tokenData, out var tokenCacheData);

                tokenCacheData.CompareId(hub.UserId());
                tokenCacheData.CompareNick(hub.Nick());
                tokenCacheData.CompareIp(hub.Ip());

                tokenCacheData.RecordSessionStart();

                _tokenStorage.Save();
            }
        }

        [Event]
        private static void OnPlayerLeft(PlayerLeftEvent ev)
        {
            if (!ev.Player.ReferenceHub.IsPlayer())
                return;

            lock (_lockObj)
            {
                TryRetrieve(ev.Player.ReferenceHub, null, out var tokenCacheData);

                tokenCacheData.RecordSessionEnd();

                _tokenStorage.Save();
                _tokens.Remove(ev.Player.ReferenceHub);
            }
        }

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRoundRestart()
        {
            _tokens.Clear();
            Plugin.Debug($"Cleared token cache.");
        }

        [UpdateEvent]
        private static void OnUpdate()
        {
            lock (_lockObj)
            {
                _tokenStorage.Data.ForEach(data => data.RecordSessionEnd());
            }
        }

        [Command("cache.remove", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        private static string CacheRemoveCommand(Player sender, string query)
        {
            if (TryRetrieveByIp(query, out var tokenCacheData)
                || TryRetrieveByUserId(query, out tokenCacheData)
                || TryRetrieveByNickname(query, 0.5, out tokenCacheData))
            {
                _tokenStorage.Remove(tokenCacheData);
                ServerLogs.AddLog(ServerLogs.Modules.Administrative, $"{sender.ReferenceHub.GetLogName(true)} removed cache record {tokenCacheData.UniqueId} ({tokenCacheData.LastNickname} | {tokenCacheData.LastId} | {tokenCacheData.LastIp}).", ServerLogs.ServerLogType.RemoteAdminActivity_Misc);
                return $"Removed cache record {tokenCacheData.UniqueId} ({tokenCacheData.LastNickname} | {tokenCacheData.LastId} | {tokenCacheData.LastIp})";
            }

            return $"Failed to find a cache record for query {query}";
        }

        [Command("cache.clear", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        private static string CacheClearCommand(Player sender, string query)
        {
            _tokenStorage.Clear();
            _tokenStorage.Save();

            ServerLogs.AddLog(ServerLogs.Modules.Administrative, $"{sender.ReferenceHub.GetLogName(true)} cleared all cache records.", ServerLogs.ServerLogType.RemoteAdminActivity_Misc);

            return "Cleared all tokens.";
        }

        [Command("cache.view", CommandType.RemoteAdmin, CommandType.GameConsole)]
        private static string CacheViewCommand(Player sender, string query)
        {
            if (TryRetrieveByIp(query, out var tokenCacheData)
                || TryRetrieveByUserId(query, out tokenCacheData)
                || TryRetrieveByNickname(query, 0.5, out tokenCacheData))
            {
                return $"Showing record for query {query}:\n" +
                    $"\n" +
                    $"《 CACHE RECORD 》\n" +
                    $"⸧ Last nickname: {tokenCacheData.LastNickname} (changed at {tokenCacheData.LastNicknameChange.ToString("F")})\n" +
                    $"⸧ Last user ID: {tokenCacheData.LastId} (changed at {tokenCacheData.LastIdChange.ToString("F")})\n" +
                    $"⸧ Last IP address: {tokenCacheData.LastIp} (changed at {tokenCacheData.LastIpChange.ToString("F")})\n" +
                    $"⸧ Last seen online: {tokenCacheData.LastJoin.ToString("F")}\n" +
                    $"⸧ Last serial: {tokenCacheData.LastSerial}\n\n" +

                    $"⸧ Total playtime: {tokenCacheData.TotalPlaytime}\n" +
                    $"⸧ Two-weeks playtime: {tokenCacheData.TwoWeeksPlaytime}\n\n" +

                    $"⸧ Record ID: {tokenCacheData.UniqueId}\n" +

                    $"\n ⧽ Nickname List ({tokenCacheData.Nicknames.Count}):\n" +
                    $"{string.Join("\n", tokenCacheData.Nicknames.Select(d => $"「{d.Value} (changed at {d.Key.ToString("F")})」"))}\n" +

                    $"\n\n ⧽ Account List ({tokenCacheData.Ids.Count}):\n" +
                    $"{string.Join("\n", tokenCacheData.Ids.Select(d => $"「{d.Value} (changed at {d.Key.ToString("F")})」"))}\n" +

                    $"\n ⧽ IP List ({tokenCacheData.Ips.Count}):\n" +
                    $"{string.Join("\n", tokenCacheData.Ips.Select(d => $"「{d.Value} (changed at {d.Key.ToString("F")})」"))}\n" +

                    $"\n\n ⧽ Sessions ({tokenCacheData.Sessions.Count}):\n" +
                    $"{string.Join("\n", tokenCacheData.Sessions.Select(d => $"「Joined: {d.Key.ToString("F")} | Left: {(d.Value <= DateTime.MinValue ? "Unrecorded" : d.Value.ToString("F"))}」"))})";
            }
            else
            {
                return $"No records were found for query: {query}";
            }
        }
    }
}