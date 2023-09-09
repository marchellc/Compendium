using Compendium.Colors;
using Compendium.Events;
using Compendium.Features;
using Compendium.Round;
using Compendium.Voice.Pools;
using Compendium.Voice.Prefabs.Scp;

using helpers.Attributes;
using helpers.Extensions;
using helpers.IO.Storage;
using helpers.Patching;
using helpers.Values;

using Mirror;

using PlayerRoles;
using PlayerRoles.Voice;

using PluginAPI.Events;

using System;
using System.Collections.Generic;
using System.Linq;

using Utils.NonAllocLINQ;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice
{
    public static class VoiceChat
    {
        private static SingleFileStorage<string> _broadcastStorage;

        private static readonly Dictionary<uint, IVoiceProfile> _activeProfiles = new Dictionary<uint, IVoiceProfile>();
        private static readonly Dictionary<uint, VoiceModifier> _activeModifiers = new Dictionary<uint, VoiceModifier>();

        private static readonly HashSet<IVoicePrefab> _activePrefabs = new HashSet<IVoicePrefab>();

        public static IReadOnlyCollection<IVoicePrefab> Prefabs => _activePrefabs;
        public static IReadOnlyCollection<IVoiceProfile> Profiles => _activeProfiles.Values;

        public static IVoiceChatState State { get; set; }

        public static void RegisterPrefab<TPrefab>() where TPrefab : IVoicePrefab, new()
        {
            if (TryGetPrefab<TPrefab>(out _))
            {
                Plugin.Warn($"Tried registering an already existing prefab.");
                return;
            }

            _activePrefabs.Add(new TPrefab());
        }

        public static void SetState(ReferenceHub hub, VoiceModifier voiceModifier)
            => _activeModifiers[hub.netId] = voiceModifier;

        public static void RemoveState(ReferenceHub hub)
            => _activeModifiers.Remove(hub.netId);

        public static Optional<VoiceModifier> GetModifiers(ReferenceHub hub)
            => _activeModifiers.TryGetValue(hub.netId, out var modifiers) ? Optional<VoiceModifier>.FromValue(modifiers) : Optional<VoiceModifier>.Null;

        public static void SetProfile(ReferenceHub hub, IVoiceProfile profile)
        {
            if (profile is null)
            {
                if (_activeProfiles.TryGetValue(hub.netId, out var curProf))
                    curProf.Disable();

                _activeProfiles.Remove(hub.netId);
                return;
            }

            _activeProfiles[hub.netId] = profile;
            profile.Enable();

            Plugin.Debug($"Set voice profile of {hub.GetLogName(false)} to {profile}");
        }

        public static void SetProfile(ReferenceHub hub, IVoicePrefab prefab)
            => SetProfile(hub, prefab.Instantiate(hub));

        public static IVoiceProfile GetProfile(ReferenceHub hub)
            => _activeProfiles.TryGetValue(hub.netId, out var profile) ? profile : null;

        public static void UnregisterPrefab<TPrefab>() where TPrefab : IVoicePrefab, new()
            => _activePrefabs.RemoveWhere(p => p is TPrefab);

        public static bool TryGetAvailableProfile(RoleTypeId role, out IVoicePrefab prefab)
            => _activePrefabs.TryGetFirst(p => p.Roles.Contains(role) || p.Roles.IsEmpty(), out prefab);

        public static bool TryGetPrefab<TPrefab>(out TPrefab prefab) where TPrefab : IVoicePrefab, new()
        {
            if (_activePrefabs.TryGetFirst(p => p is TPrefab, out var prefabValue) && prefabValue is TPrefab castPrefab)
            {
                prefab = castPrefab;
                return true;
            }

            prefab = default;
            return false;
        }

        [Load]
        [Reload]
        private static void Load()
        {
            if (_broadcastStorage != null)
            {
                _broadcastStorage.Reload();
                return;
            }

            RegisterPrefab<ScpVoicePrefab>();

            _broadcastStorage = new SingleFileStorage<string>($"{Directories.ThisData}/SavedVoiceBroadcasts");
            _broadcastStorage.Load();

            Plugin.Info($"Voice Chat system loaded.");
        }

        [Unload]
        private static void Unload()
        {
            UnregisterPrefab<ScpVoicePrefab>();

            _activeModifiers.Clear();
            _activePrefabs.Clear();
            _activeProfiles.Clear();

            _broadcastStorage.Save();
            _broadcastStorage = null;

            State = null;

            Plugin.Info("Voice Chat system unloaded.");
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnWaiting()
        {
            _activeProfiles.Clear();
            _activeModifiers.Clear();

            State = null;
        }

        [Event]
        private static void OnRoleChanged(PlayerChangeRoleEvent ev)
        {
            Calls.Delay(0.5f, () =>
            {
                if (TryGetAvailableProfile(ev.NewRole, out var prefab))
                    SetProfile(ev.Player.ReferenceHub, prefab);
                else
                {
                    var found = false;

                    if (found = (_activeProfiles.TryGetValue(ev.Player.NetworkId, out var curProf)))
                        curProf.Disable();

                    _activeProfiles.Remove(ev.Player.NetworkId);

                    if (found)
                        Plugin.Debug($"Removed voice profile from {ev.Player.ReferenceHub.GetLogName()}: {curProf}");
                }
            });
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (_broadcastStorage.Data.Contains(ev.Player.UserId))
                return;

            ev.Player.ReferenceHub.Message(
                $"\nVítej na serveru Peanut Club!\n" +
                $"Využíváme zde pár funkcí, které vyžadují záznam kláves od hráčů.\n" +
                $"Tyto funkce aktuálně zahrnují pouze možnost přepínaní SCP voice chatu na Proximity a zpět, ale později jich bude mnohem více.\n\n" +
                $"Pro povolení záznamu kláves musíš spustit hru s launch argumentem -allow-syncbind (ten můžeš nastavit když ve Steam knihovně klikneš na hru pravým tlačítkem myši, vybereš Vlastnosti a otevřeš záložku Obecné, kde se nachází textové pole úplně dole, do kterého to napíšeš).\n" +
                $"Poté vyžaduje hra ještě potvrzení, které můžeš provést tím, že do této konzole napíšeš dvakrát synccmd (můžeš provést i teď, nebo potom).\n" +
                $"Toť vše, užij si hru!");

            ev.Player.ReferenceHub.Hint(
                $"\n\n\n" +
                $"<b><color={ColorValues.LightGreen}>Vítej! Tuto zprávu uvidíš jen jednou.\n" +
                $"Na tomto serveru máme pár funkcí, které závisí na bindování. Pro více informací si otevři <color={ColorValues.Red}>herní konzoli</color>\n" +
                $"<i>(<color={ColorValues.Green}>klávesa nad tabulátorem a pod Escapem: ~</color></i>" +
                $"\n</color></b>", 7f, true);

            _broadcastStorage.Add(ev.Player.UserId);
        }

        [Patch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
        private static bool Patch(NetworkConnection conn, VoiceMessage msg)
        {
            try
            {
                if (msg.SpeakerNull || msg.Speaker.netId != conn.identity.netId)
                    return false;

                if (!(msg.Speaker.Role() is IVoiceRole speakerRole))
                    return false;

                if (!VoiceChatUtils.CheckRateLimit(speakerRole.VoiceModule))
                    return false;

                if (VoiceChatMutes.IsMuted(msg.Speaker))
                    return false;

                var sendChannel = speakerRole.VoiceModule.ValidateSend(msg.Channel);
                var packet = VoiceChatUtils.GeneratePacket(msg, speakerRole, sendChannel);

                if (State is null || !State.Process(packet))
                {
                    var profile = GetProfile(packet.Speaker);

                    if (profile != null)
                        profile.Process(packet);
                }

                if (packet.SenderChannel != VoiceChatChannel.None)
                {
                    speakerRole.VoiceModule.CurrentChannel = packet.SenderChannel;
                    packet.Destinations.ForEach(p =>
                    {
                        if (p.Value != VoiceChatChannel.None)
                        {
                            msg.Channel = p.Value;
                            p.Key.connectionToClient.Send(msg);
                        }
                    });
                }

                PacketPool.Pool.Push(packet);
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Error($"The voice chat patch caught an exception!");
                Plugin.Error(ex);

                return true;
            }
        }
    }
}