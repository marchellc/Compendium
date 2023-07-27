using BetterCommands;

using Compendium.Helpers;
using Compendium.Helpers.Events;
using Compendium.Helpers.Round;
using Compendium.Helpers.Token;

using Compendium.IdCache;

using helpers.Attributes;
using helpers.Extensions;
using helpers.IO.Storage;
using helpers.Json;

using PluginAPI.Core;
using PluginAPI.Enums;
using PluginAPI.Events;
using PluginAPI.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.TokenCache
{
    public static class TokenCacheHandler
    {
        private static IStorageBase _tokenStorage;
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

        public static bool TryGetRealIp(string userId, out string realIp)
        {
            if (TryRetrieveByUserId(userId, out var cache))
            {
                realIp = cache.LastIp;
                return true;
            }

            realIp = null;
            return false;
        }

        public static bool TryRetrieveByToken(string token, out TokenCacheData tokenCacheData)
        {
            if (!TokenParser.TryParse(token, out var tokenData))
            {
                tokenCacheData = null;
                return false;
            }

            if (_tokenStorage.TryFirst(cache => cache.EhId == tokenData.EhId || cache.Public == tokenData.PublicPart || cache.Signature == tokenData.Signature, out tokenCacheData))
                return true;

            tokenCacheData = null;
            return false;
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
                tokenCacheData.RecordIpChange(tokenData.Ip);

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

            ServerEventType.PlayerJoined.AddHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);
            ServerEventType.PlayerLeft.AddHandler<Action<PlayerLeftEvent>>(OnPlayerLeft);
        }

        [Unload]
        private static void Unload()
        {
            _tokenStorage.Save();

            ServerEventType.PlayerJoined.RemoveHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);
            ServerEventType.PlayerLeft.RemoveHandler<Action<PlayerLeftEvent>>(OnPlayerLeft);
        }

        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (!ev.Player.ReferenceHub.IsPlayer())
                return;

            TryGetToken(ev.Player.ReferenceHub, out var tokenData);
            TryRetrieveOrAdd(ev.Player.ReferenceHub, ref tokenData, out var tokenCacheData);

            tokenCacheData.CompareId(ev.Player.UserId);
            tokenCacheData.CompareNick(ev.Player.Nickname);
            tokenCacheData.CompareIp(tokenData.Ip);

            tokenCacheData.RecordSessionStart();

            _tokenStorage.Save();
        }

        private static void OnPlayerLeft(PlayerLeftEvent ev)
        {
            if (!ev.Player.ReferenceHub.IsPlayer())
                return;
            
            TryRetrieve(ev.Player.ReferenceHub, null, out var tokenCacheData);

            tokenCacheData.RecordSessionEnd();

            _tokenStorage.Save();
            _tokens.Remove(ev.Player.ReferenceHub);
        }

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRoundRestart()
        {
            _tokens.Clear();
            Plugin.Debug($"Cleared token cache.");
        }

        [Command("cache", BetterCommands.CommandType.RemoteAdmin, BetterCommands.CommandType.GameConsole)]
        private static string CacheCommand(Player sender, string query)
        {
            if (TryRetrieveByIp(query, out var tokenCacheData)
                || TryRetrieveByUserId(query, out tokenCacheData)
                || TryRetrieveByNickname(query, 0.2, out tokenCacheData))
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