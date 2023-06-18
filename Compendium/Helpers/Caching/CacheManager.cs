using BetterCommands;

using Compendium.Attributes;
using Compendium.Helpers.Events;

using helpers;
using helpers.Extensions;
using helpers.IO.Binary;
using helpers.Random;

using PluginAPI.Enums;
using PluginAPI.Events;
using PluginAPI.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandType = BetterCommands.CommandType;

namespace Compendium.Helpers.Caching
{
    [LogSource("Cache Manager")]
    public static class CacheManager
    {
        private static readonly List<CacheData> _cache = new List<CacheData>();
        private static readonly object _lock = new object();

        public static string GlobalPath => $"{Paths.Configs}/cache.dat";
        public const int UniqueIdLength = 7;

        [InitOnLoad]
        public static void Initialize()
        {
            Plugin.OnReloaded.Register((Action)Reload);
            Plugin.OnUnloaded.Register((Action)Save);

            ServerEventType.PlayerJoined.AddHandler<Action<PlayerJoinedEvent>>(HandlePlayerJoin);

            Reload();
        }

        public static void Reload()
        {
            lock (_lock)
            {
                var file = new BinaryImage();

                _cache.Clear();

                file.Load(GlobalPath);

                if (!file.TryRetrieve<List<CacheData>>(out var saved))
                {
                    Save();
                    return;
                }

                _cache.AddRange(saved);
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                var file = new BinaryImage();

                file.Store(_cache);
                file.Save(GlobalPath);
            }
        }

        public static bool TryGet(string value, out CacheData cacheData)
        {
            cacheData = _cache.FirstOrDefault(x => CompareCache(value, x, true));
            return cacheData != null;
        }

        public static bool TryGetExact(string value, out CacheData cacheData)
        {
            cacheData = _cache.FirstOrDefault(x => CompareCache(value, x));
            return cacheData != null;
        }

        public static string GetIdByIp(string ip)
        {
            if (!TryGetExact(ip, out var data))
            {
                return null;
            }           

            return data.UniqueId;
        }

        public static string GetIpById(string id)
        {
            if (!TryGetExact(id, out var data))
            {
                return null;
            }

            return data.Ip;
        }

        public static string GetOrAddIp(ReferenceHub hub) => GetOrAdd(hub).Ip;
        public static string GetOrAddId(ReferenceHub hub) => GetOrAdd(hub).UniqueId;

        public static CacheData GetOrAdd(ReferenceHub hub)
        {
            if (!TryGetExact(hub.connectionToClient.address, out var cacheData) 
                && !TryGetExact(hub.characterClassManager.UserId, out cacheData)) 
                cacheData = Add(hub);

            return cacheData;
        }

        public static CacheData Add(ReferenceHub hub)
        {
            var data = new CacheData
            {
                Ip = hub.connectionToClient.address,
                LastId = hub.characterClassManager.UserId,
                LastName = hub.nicknameSync.Network_myNickSync.Trim(),
                LastOnline = DateTime.Now.ToLocalTime()
            };

            GenerateUniqueId(x =>
            {
                data.UniqueId = x;
                _cache.Add(data);
                Save();
            });

            var localTime = DateTime.Now.ToLocalTime();

            data.AllIds.Add(hub.characterClassManager.UserId, localTime);
            data.AllNames.Add(hub.nicknameSync.Network_myNickSync.Trim(), localTime);

            return data;
        }

        public static void GenerateUniqueId(Action<string> callback)
        {
            Task.Run(() =>
            {
                var id = GenerateUniqueIdCallback();

                callback?.Invoke(id);

                Plugin.Debug($"Generated unique ID: {id}");
            });
        }

        private static string GenerateUniqueIdCallback()
        {
            var generatedId = RandomGeneration.Default.GetReadableString(UniqueIdLength);

            while (_cache.Any(x => x.UniqueId == generatedId)) 
                generatedId = RandomGeneration.Default.GetReadableString(UniqueIdLength);

            return generatedId;
        }

        private static bool CompareCache(string value, CacheData data, bool compareName = false)
        {
            Plugin.Debug($"Comparing {value} to \n{data}");

            if (data.Ip == value || data.UniqueId == value || data.LastId == value) 
                return true;

            if (data.AllIds.Any(x => x.Key == value)) 
                return true;

            if (compareName)
            {
                if (data.LastName.GetSimilarity(value) > 0.5)
                    return true;

                if (data.AllNames.Any(name => name.Key.GetSimilarity(value) > 0.5))
                    return true;
            }

            return false;
        }

        private static void HandlePlayerJoin(PlayerJoinedEvent ev)
        {
            var player = ev.Player.ReferenceHub;
            var data = GetOrAdd(player);

            Plugin.Debug($"Player joined\n{data}");

            if (data.LastId != player.characterClassManager.UserId)
            {
                data.LastId = player.characterClassManager.UserId;
                data.AllIds.Add(player.characterClassManager.UserId, DateTime.Now.ToLocalTime());
            }

            var nick = player.nicknameSync.Network_myNickSync.Trim();

            if (data.LastName != nick)
            {
                data.LastName = nick;
                data.AllNames.Add(nick, DateTime.Now.ToLocalTime());
            }

            data.LastOnline = DateTime.Now.ToLocalTime();

            Save();
        }

        [Command("cache", CommandType.RemoteAdmin, CommandType.GameConsole)]
        private static string CacheCommand(ReferenceHub sender, string targetId)
        {
            if (TryGet(targetId, out var cache))
            {
                var builder = new StringBuilder();

                builder.AppendLine($"<color=#E0FF33>「Cache Record」</color>");
                builder.AppendLine($"‣ <color=#33FFA5>Nickname</color>: {cache.LastName}");
                builder.AppendLine($"‣ <color=#33FFA5>User ID</color>: {cache.LastId}");
                builder.AppendLine($"‣ <color=#33FFA5>Unique ID</color>: {cache.UniqueId}");
                builder.AppendLine($"‣ <color=#33FFA5>IP address</color>: {cache.Ip}");
                builder.AppendLine($"‣ <color=#33FFA5>Last seen</color>: {cache.LastOnline.ToString("F")}");

                builder.AppendLine();
                builder.AppendLine($"⇛ Nickname list ({cache.AllNames.Count})");

                foreach (var nick in cache.AllNames)
                {
                    builder.AppendLine($"➣ <color=#33FFA5>{nick.Key}</color> 〔{nick.Value.ToString("F")}〕");
                }

                builder.AppendLine();
                builder.AppendLine($"⇛ Account list ({cache.AllIds.Count})");

                foreach (var id in cache.AllIds)
                {
                    builder.AppendLine($"➣ <color=#33FFA5>{id.Key}</color> 〔{id.Value.ToString("F")}〕");
                }

                return builder.ToString();
            }
            else
                return $"Failed to find a cache record for {targetId}";
        }
    }
}
