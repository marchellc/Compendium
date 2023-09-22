using BetterCommands;

using Compendium.Colors;
using Compendium.Events;
using Compendium.Round;
using Compendium.Voice.Pools;
using Compendium.Voice.Prefabs.Scp;

using helpers.Attributes;
using helpers.Extensions;
using helpers.Patching;

using Mirror;

using PlayerRoles;
using PlayerRoles.Voice;

using PluginAPI.Events;

using System;
using System.Collections.Generic;

using Utils.NonAllocLINQ;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice
{
    public static class VoiceChat
    {
        private static readonly Dictionary<uint, IVoiceProfile> _activeProfiles = new Dictionary<uint, IVoiceProfile>();
        private static readonly Dictionary<uint, List<VoiceModifier>> _activeModifiers = new Dictionary<uint, List<VoiceModifier>>();

        private static readonly HashSet<uint> _speakCache = new HashSet<uint>();
        private static readonly HashSet<IVoicePrefab> _activePrefabs = new HashSet<IVoicePrefab>();

        public static IReadOnlyCollection<IVoicePrefab> Prefabs => _activePrefabs;
        public static IReadOnlyCollection<IVoiceProfile> Profiles => _activeProfiles.Values;

        public static IVoiceChatState State { get; set; }

        public static event Action<ReferenceHub> OnStartedSpeaking;
        public static event Action<ReferenceHub> OnStoppedSpeaking;

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
        {
            if (!_activeModifiers.ContainsKey(hub.netId))
                _activeModifiers[hub.netId] = new List<VoiceModifier>() { voiceModifier };
            else
                _activeModifiers[hub.netId].Add(voiceModifier);
        }

        public static void RemoveState(ReferenceHub hub, VoiceModifier voiceModifier)
        {
            if (!_activeModifiers.ContainsKey(hub.netId))
                return;

            _activeModifiers[hub.netId].Remove(voiceModifier);
        }

        public static List<VoiceModifier> GetModifiers(ReferenceHub hub)
            => _activeModifiers.TryGetValue(hub.netId, out var modifiers) ? modifiers : null;

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

        private static bool IsSpeaking(ReferenceHub hub)
            => _speakCache.Contains(hub.netId);

        private static void SetSpeaking(ReferenceHub hub, bool state)
        {
            if (!state)
                _speakCache.Remove(hub.netId);
            else
                _speakCache.Add(hub.netId);
        }

        [Load]
        [Reload]
        private static void Load()
        {
            RegisterPrefab<ScpVoicePrefab>();
            Plugin.Info($"Voice Chat system loaded.");
        }

        [Unload]
        private static void Unload()
        {
            UnregisterPrefab<ScpVoicePrefab>();

            _activeModifiers.Clear();
            _activePrefabs.Clear();
            _activeProfiles.Clear();
            _speakCache.Clear();

            State = null;

            Plugin.Info("Voice Chat system unloaded.");
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnWaiting()
        {
            _activeProfiles.Clear();
            _activeModifiers.Clear();
            _speakCache.Clear();

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
                    if (_activeProfiles.TryGetValue(ev.Player.NetworkId, out var curProf))
                        curProf.Disable();

                    _activeProfiles.Remove(ev.Player.NetworkId);
                }
            });
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            Calls.Delay(1.5f, () =>
            {
                ev.Player.ReferenceHub.Message(
                    $"\nVítej na serveru Peanut Club!\n" +
                    $"Využíváme zde pár funkcí, které vyžadují záznam kláves od hráčů.\n" +
                    $"Tyto funkce aktuálně zahrnují pouze možnost přepínaní SCP voice chatu na Proximity a zpět, ale později jich bude mnohem více.\n\n" +
                    $"Pro povolení záznamu kláves musíš spustit hru s launch argumentem -allow-syncbind (ten můžeš nastavit když ve Steam knihovně klikneš na hru pravým tlačítkem myši, vybereš Vlastnosti a otevřeš záložku Obecné, kde se nachází textové pole úplně dole, do kterého to napíšeš).\n" +
                    $"Poté vyžaduje hra ještě potvrzení, které můžeš provést tím, že do této konzole napíšeš dvakrát synccmd (můžeš provést i teď, nebo potom).\n" +
                    $"Toť vše, užij si hru!");

                ev.Player.ReferenceHub.Hint(
                    $"\n\n\n" +
                    $"<b><color={ColorValues.LightGreen}>Vítej!\n" +
                    $"Na tomto serveru máme pár funkcí, které závisí na bindování. Pro více informací si otevři <color={ColorValues.Red}>herní konzoli</color>\n" +
                    $"<i>(<color={ColorValues.Green}>klávesa nad tabulátorem a pod Escapem: ~)</color></i>" +
                    $"\n</color></b>", 15f, true);
            });
        }

        [UpdateEvent]
        private static void OnUpdate()
        {
            foreach (var hub in Hub.Hubs)
            {
                if (!(hub.Role() is IVoiceRole vcRole))
                    continue;

                if (vcRole.VoiceModule is null)
                    continue;

                if (vcRole.VoiceModule.ServerIsSending)
                {
                    if (!IsSpeaking(hub))
                    {
                        SetSpeaking(hub, true);
                        OnStartedSpeaking?.Invoke(hub);
                    }
                }
                else
                {
                    if (IsSpeaking(hub))
                    {
                        SetSpeaking(hub, false);
                        OnStoppedSpeaking?.Invoke(hub);
                    }
                }
            }
        }

        [BetterCommands.Command("playback", CommandType.PlayerConsole)]
        [Description("Enables or disables microphone playback.")]
        private static string PlaybackCommand(ReferenceHub sender)
        {
            var cur = GetModifiers(sender);

            if (cur != null && cur.Contains(VoiceModifier.PlaybackEnabled))
            {
                RemoveState(sender, VoiceModifier.PlaybackEnabled);
                return "Disabled playback.";
            }
            else
            {
                SetState(sender, VoiceModifier.PlaybackEnabled);
                return "Enabled playback.";
            }
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
                        if (packet.AlternativeSenders.TryGetValue(p.Key, out var sender))
                            msg.Speaker = sender;
                        else if (msg.Speaker.netId != packet.Speaker.netId)
                            msg.Speaker = packet.Speaker;

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