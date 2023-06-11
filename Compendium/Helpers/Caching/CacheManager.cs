using Compendium.Attributes;
using Compendium.Helpers.Events;

using helpers;
using helpers.IO.Binary;
using helpers.Random;

using PluginAPI.Core;
using PluginAPI.Enums;
using PluginAPI.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compendium.Helpers.Caching
{
    [LogSource("Cache Manager")]
    public static class CacheManager
    {
        private static readonly BinaryImage _file = new BinaryImage();
        private static readonly List<CacheData> _cache = new List<CacheData>();
        private static readonly object _lock = new object();

        public static string GlobalPath => $"{Paths.GlobalPlugins}/cache.dat";
        public const int UniqueIdLength = 7;

        [InitOnLoad]
        public static void Initialize()
        {
            Plugin.Info($"Global cache path set to: {GlobalPath}");
            Plugin.OnReloaded.Add(Reload);
            Plugin.OnUnloaded.Add(Save);

            ServerEventType.PlayerJoined.GetProvider()?.Add(HandlePlayerJoin);

            Reload();
        }

        public static void Reload()
        {
            lock (_lock)
            {
                _cache.Clear();
                _file.Load(GlobalPath);
                _cache.AddRange(_file.Retrieve<List<CacheData>>());
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                _file.Store(_cache);
                _file.Save(GlobalPath);
            }
        }

        public static bool TryGet(string value, out CacheData cacheData)
        {
            cacheData = _cache.FirstOrDefault(x => CompareCache(value, x));
            return cacheData != null;
        }

        public static CacheData GetOrAdd(ReferenceHub hub)
        {
            if (!TryGet(hub.connectionToClient.address, out var cacheData) && !TryGet(hub.characterClassManager.UserId, out cacheData)) cacheData = Add(hub);
            return cacheData;
        }

        public static CacheData Add(ReferenceHub hub)
        {
            var data = new CacheData
            {
                Ip = hub.connectionToClient.address,
                LastId = hub.characterClassManager.UserId,
                LastName = hub.nicknameSync.Network_myNickSync
            };

            GenerateUniqueId(x =>
            {
                data.UniqueId = x;
                Save();
            });

            var localTime = DateTime.Now.ToLocalTime();

            data.AllIds.Add(hub.characterClassManager.UserId, localTime);
            data.AllNames.Add(hub.nicknameSync.Network_myNickSync, localTime);

            return data;
        }

        public static void GenerateUniqueId(Action<string> callback)
        {
            Task.Run(() =>
            {
                callback?.Invoke(GenerateUniqueIdCallback());
            });
        }

        private static string GenerateUniqueIdCallback()
        {
            var generatedId = RandomGeneration.Default.GetReadableString(UniqueIdLength);
            while (_cache.Any(x => x.UniqueId == generatedId)) generatedId = RandomGeneration.Default.GetReadableString(UniqueIdLength);
            return generatedId;
        }

        private static bool CompareCache(string value, CacheData data, bool compareName = false)
        {
            if (data.Ip == value || data.UniqueId == value || data.LastId == value || (compareName && data.LastName.ToLower() == value.ToLower())) return true;
            if (data.AllNames.Any(x => x.Key == value) || data.AllIds.Any(x => x.Key == value) || (compareName && data.AllNames.Any(x => x.Key.ToLower() == value.ToLower()))) return true;
            return false;
        }

        private static void HandlePlayerJoin(ObjectCollection eventArgsCollection)
        {
            var player = eventArgsCollection.Get<Player>("player");
            var data = GetOrAdd(player.ReferenceHub);

            if (data.LastId != player.UserId)
            {
                data.LastId = player.UserId;
                data.AllIds.Add(player.UserId, DateTime.Now.ToLocalTime());
            }

            if (data.LastName != player.Nickname)
            {
                data.LastName = player.Nickname;
                data.AllNames.Add(player.Nickname, DateTime.Now.ToLocalTime());
            }

            data.LastOnline = DateTime.Now;

            Save();
        }
    }
}
