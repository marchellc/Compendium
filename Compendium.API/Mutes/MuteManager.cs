using Compendium.Events;
using Compendium.Generation;
using Compendium.IO.Saving;
using Compendium.PlayerData;
using Compendium.Updating;

using helpers;
using helpers.Attributes;
using helpers.Pooling.Pools;

using PluginAPI.Events;

using System;
using System.Linq;

using VoiceChat;

namespace Compendium.Mutes
{
    public static class MuteManager
    {
        private static SaveFile<CollectionSaveData<Mute>> Mutes;
        private static SaveFile<CollectionSaveData<Mute>> History;

        private static object LockObject = new object();

        public static event Action<Mute> OnExpired;
        public static event Action<Mute> OnIssued;

        [Load]
        public static void Load()
        {
            Mutes ??= new SaveFile<CollectionSaveData<Mute>>(Directories.GetDataPath("SavedMutes", "mutes"));
            History ??= new SaveFile<CollectionSaveData<Mute>>(Directories.GetDataPath("SavedMuteHistory", "muteHistory"));

            OnExpired += m => Plugin.Info($"Mute '{m.Id}' ({m.IssuerId} -> {m.TargetId}) for '{m.Reason}' (at {new DateTime(m.IssuedAt).ToString("G")}) has expired.");
            OnIssued += m => Plugin.Info($"Mute '{m.Id}' ({m.IssuerId} -> {m.TargetId}) for '{m.Reason}' (at {new DateTime(m.IssuedAt).ToString("G")}) has been issued.");
        }

        public static bool Remove(Mute mute)
        {
            lock (LockObject)
            {
                if (!Mutes.Data.Contains(mute))
                    return false;

                Mutes.Data.Remove(mute);
                Mutes.Save();

                History.Data.Add(mute);
                History.Save();

                OnExpired?.Invoke(mute);

                return true;
            }
        }

        public static bool RemoveAll(ReferenceHub target)
        {
            lock (LockObject)
            {
                if (!Mutes.Data.Any(m => m.TargetId == target.UserId()))
                    return false;

                var mutes = Mutes.Data.Where(m => m.TargetId == target.UserId());

                foreach (var mute in mutes)
                {
                    Mutes.Data.Remove(mute);
                    Mutes.Save();

                    History.Data.Add(mute);
                    History.Save();

                    OnExpired?.Invoke(mute);
                }

                return true;
            }
        }

        public static Mute Query(string id)
            => Mutes.Data.FirstOrDefault(m => m.Id == id);

        public static Mute[] Query(ReferenceHub target)
            => Mutes.Data.Where(m => m.TargetId == target.UserId()).ToArray();

        public static Mute[] Query(PlayerDataRecord record)
            => Mutes.Data.Where(m => m.TargetId == record.UserId).ToArray();

        public static Mute[] QueryHistory(ReferenceHub target)
            => History.Data.Where(m => m.TargetId == target.UserId()).ToArray();

        public static Mute[] QueryHistory(PlayerDataRecord record)
            => History.Data.Where(m => m.TargetId == record.UserId).ToArray();

        public static Mute[] QueryIssued(ReferenceHub issuer)
            => Mutes.Data.Where(m => m.IssuerId == issuer.UserId()).Concat(History.Data.Where(m => m.IssuerId == issuer.UserId())).ToArray();

        public static Mute[] QueryIssued(PlayerDataRecord record)
            => Mutes.Data.Where(m => m.IssuerId == record.UserId).Concat(History.Data.Where(m => m.IssuerId == record.UserId)).ToArray();

        public static Mute[] QueryAll()
            => Mutes.Data.ToArray();

        public static Mute[] QueryHistory()
            => History.Data.ToArray();

        public static Mute[] QueryAllWithHistory()
            => Mutes.Data.Concat(History.Data).ToArray();

        public static bool Issue(ReferenceHub issuer, ReferenceHub target, string reason, TimeSpan duration)
        {
            if (duration.TotalMilliseconds <= 0)
                return false;

            if (target is null)
                return false;

            if (string.IsNullOrWhiteSpace(reason))
                reason = "Not specified";

            if (issuer == null)
                issuer = ReferenceHub.HostHub;

            var mute = new Mute
            {
                Id = UniqueIdGeneration.Generate(3),

                ExpiresAt = (DateTime.Now + duration).Ticks,
                IssuedAt = DateTime.Now.Ticks,

                IssuerId = issuer.UserId(),
                TargetId = target.UserId(),

                Reason = reason
            };

            lock (LockObject)
            {
                Mutes.Data.Add(mute);
                Mutes.Save();
            }

            VoiceChatMutes.SetFlags(target, VcMuteFlags.LocalRegular | VcMuteFlags.LocalIntercom);

            OnIssued?.Invoke(mute);

            return true;
        }

        public static bool Issue(ReferenceHub issuer, PlayerDataRecord target, string reason, TimeSpan duration)
        {
            if (duration.TotalMilliseconds <= 0)
                return false;

            if (target is null)
                return false;

            if (string.IsNullOrWhiteSpace(reason))
                reason = "Not specified";

            if (issuer == null)
                issuer = ReferenceHub.HostHub;

            var mute = new Mute
            {
                Id = UniqueIdGeneration.Generate(3),

                ExpiresAt = (DateTime.Now + duration).Ticks,
                IssuedAt = DateTime.Now.Ticks,

                IssuerId = issuer.UserId(),
                TargetId = target.UserId,

                Reason = reason
            };

            lock (LockObject)
            {
                Mutes.Data.Add(mute);
                Mutes.Save();
            }

            OnIssued?.Invoke(mute);

            if (target.TryGetHub(out var hub))
                VoiceChatMutes.SetFlags(hub, VcMuteFlags.LocalRegular | VcMuteFlags.LocalIntercom);

            return true;
        }

        [Update(Delay = 1000)]
        private static void Update()
        {
            if (Mutes is null || History is null)
                return;

            lock (LockObject)
            {
                var expired = ListPool<Mute>.Pool.Get();

                for (int i = 0; i < Mutes.Data.Count; i++)
                {
                    if (Mutes.Data[i].IsExpired())
                        expired.Add(Mutes.Data[i]);
                }

                if (expired.Count > 0)
                {
                    for (int i = 0; i < expired.Count; i++)
                    {
                        Mutes.Data.Remove(expired[i]);
                        History.Data.Add(expired[i]);

                        OnExpired?.Invoke(expired[i]);

                        Mutes.Save();
                        History.Save();
                    }

                    for (int i = 0; i < expired.Count; i++)
                    {
                        if (Hub.TryGetHub(expired[i].TargetId, out var hub) 
                            && Query(hub).Length <= 0)
                            VoiceChatMutes.SetFlags(hub, VcMuteFlags.None);
                    }
                }

                expired.ReturnList();
            }
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (Query(ev.Player.ReferenceHub).Length > 0)
                VoiceChatMutes.SetFlags(ev.Player.ReferenceHub, VcMuteFlags.LocalIntercom | VcMuteFlags.LocalRegular);
        }
    }
}
