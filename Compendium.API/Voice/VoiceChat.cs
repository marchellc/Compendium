using Compendium.Events;
using Compendium.Round;
using Compendium.Voice.Pools;
using Compendium.Voice.Prefabs.Scp;

using helpers.Attributes;
using helpers.Extensions;
using helpers.Patching;
using helpers.Values;

using Mirror;

using PlayerRoles;
using PlayerRoles.Voice;
using PluginAPI.Events;
using System.Collections.Generic;

using Utils.NonAllocLINQ;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice
{
    public static class VoiceChat
    {
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
            => _activeProfiles[hub.netId] = profile;

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
        private static void Load()
        {
            RegisterPrefab<ScpVoicePrefab>();
        }

        [Unload]
        private static void Unload()
        {
            UnregisterPrefab<ScpVoicePrefab>();

            _activeModifiers.Clear();
            _activePrefabs.Clear();
            _activeProfiles.Clear();

            State = null;
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
            if (TryGetAvailableProfile(ev.NewRole, out var prefab))
                SetProfile(ev.Player.ReferenceHub, prefab);
            else
                _activeProfiles[ev.Player.NetworkId] = null;
        }

        [Patch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))]
        private static bool Patch(NetworkConnection conn, VoiceMessage msg)
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
    }
}