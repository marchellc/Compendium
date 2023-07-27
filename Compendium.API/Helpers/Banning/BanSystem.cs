using Compendium.TokenCache;

using helpers.Attributes;
using helpers.Events;
using helpers.Extensions;
using helpers.IO.Storage;
using helpers.Json;
using helpers.Pooling.Pools;

using PluginAPI.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Helpers.Banning
{
    public static class BanSystem
    {
        private static HashSet<BanData> _bans = new HashSet<BanData>();
        private static IStorageBase _banFile;

        public static bool IsActive => Plugin.Config.BanSettings.UseCustom;

        public static readonly EventProvider OnBanIssued = new EventProvider();
        public static readonly EventProvider OnBanRemoved = new EventProvider();

        public static bool TryGetBan(string targetId, out BanData ban)
            => _bans.TryGetFirst(b => b.IssuedTo == targetId || b.Id == targetId, out ban);

        public static void Issue(ReferenceHub issuer, ReferenceHub target, string reason, long durationSeconds)
        {
            if (!TokenCacheHandler.TryRetrieve(target, null, out var tokenCache))
            {
                Plugin.Warn($"Failed to issue ban: failed to retrieve token cache data.");
                return;
            }

            Issue(issuer, tokenCache, reason, durationSeconds);
        }

        public static void Issue(ReferenceHub issuer, TokenCacheData target, string reason, long durationSeconds)
        {
            var ban = new BanData();

            ban.IssuedAt = DateTime.Now.ToLocalTime();
            ban.EndsAt = ban.IssuedAt + TimeSpan.FromSeconds(durationSeconds);
            ban.IssuedTo = target.UniqueId;

            if (TokenCacheHandler.TryRetrieve(issuer, null, out var issuerToken))
                ban.IssuedBy = issuerToken.UniqueId;
            else
                ban.IssuedBy = "server";

            ban.Reason = reason;

            if (TryGetBan(target.UniqueId, out _))
                Remove(target.UniqueId, BanRemovalReason.Overriding);

            IssueInternal(ban);
        }

        public static void Remove(string id, BanRemovalReason reason = BanRemovalReason.Expired)
        {
            lock (_bans)
            {
                if (TryGetBan(id, out var ban))
                {
                    _bans.Remove(ban);

                    OnBanRemoved.Invoke(ban, reason);

                    Save();

                    Plugin.Debug($"Removed ban ({reason}):\n{ban.ToJson()}");
                }
                else
                {
                    Plugin.Warn($"Attempted to remove ban with unknown ID: {id}");
                }
            }
        }

        [Load]
        [Reload]
        public static void Load()
        {
            if (_banFile != null)
            {
                Save();
                _banFile = null;
            }

            _banFile = new SingleFileStorage<BanData>(Plugin.Config.BanSettings.IsGlobal ? $"{Paths.SecretLab}/ban_storage" : $"{Plugin.Handler.PluginDirectoryPath}/ban_storage");
            _banFile.Load();
            _bans = _banFile.GetAll<BanData>().ToHashSet();

            Plugin.Debug($"Bans loaded. (file: \"{_banFile.Path}\")");
        }

        [Unload]
        public static void Save()
        {
            if (_banFile != null)
                _banFile.Save();

            Plugin.Debug($"Bans saved.");
        }

        private static void IssueInternal(BanData banData)
        {
            if (_banFile is null)
            {
                Plugin.Warn($"Tried issuing a ban, but the ban file is invalid!");
                return;
            }

            _banFile.Clear();
            _bans.ForEach(b => _banFile.Add(b, false));

            Save();

            Plugin.Debug($"Ban issued:\n{banData.ToJson()}");
        }

        private static void OnUpdate()
        {
            lock (_bans)
            {
                var bansToRemove = ListPool<BanData>.Pool.Get();

                _bans.ForEach(ban =>
                {
                    if (DateTime.Now.ToLocalTime() >= ban.EndsAt)
                    {
                        bansToRemove.Add(ban);
                    }
                });

                bansToRemove.ForEach(ban => Remove(ban.Id, BanRemovalReason.Expired));
            }
        }
    }
}
