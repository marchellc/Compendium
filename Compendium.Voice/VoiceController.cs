using Compendium.Features;
using Compendium.Helpers.Events;
using Compendium.Voice.Prefabs;
using Compendium.Voice.Profiles;

using helpers;
using helpers.Extensions;

using PlayerRoles;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;

using VoiceChat;

namespace Compendium.Voice
{
    public static class VoiceController
    {
        internal static bool _isRestarting;
        internal static readonly Dictionary<uint, OverwatchVoiceFlags> m_OvFlags = new Dictionary<uint, OverwatchVoiceFlags>();

        private static readonly Dictionary<uint, IVoiceProfile> m_Profiles = new Dictionary<uint, IVoiceProfile>();

        internal static readonly HashSet<uint> m_Playback = new HashSet<uint>();

        private static readonly HashSet<IVoicePrefab> m_ProfilePrefabs = new HashSet<IVoicePrefab>()
        {
            new ScpProfilePrefab()
        };

        public static bool IsActive { get; private set; }

        public static IReadOnlyCollection<IVoicePrefab> Prefabs => m_ProfilePrefabs;

        public static IReadOnlyCollection<uint> Playback => m_Playback;

        public static IReadOnlyDictionary<uint, IVoiceProfile> Profiles => m_Profiles;
        public static IReadOnlyDictionary<uint, OverwatchVoiceFlags> OverwatchFlags => m_OvFlags;

        public static ReferenceHub PriorityVoice { get; set; }

        public static StaffVoiceFlags StaffFlags { get; set; } = StaffVoiceFlags.None;

        public static void Load()
        {
            if (IsActive)
            {
                FLog.Warn($"The Voice Controller is already active!");
                return;
            }

            IsActive = true;

            RegisterEvents();

            FLog.Info("Voice Controller loaded.");
        }

        public static void Unload()
        {
            if (!IsActive)
            {
                FLog.Warn($"The Voice Controller is not active!");
                return;
            }

            IsActive = false;

            UnregisterEvents();

            FLog.Warn("Voice Controller unloaded.");
        }

        public static bool TryGetProfile(ReferenceHub hub, out IVoiceProfile voiceProfile)
            => m_Profiles.TryGetValue(hub.netId, out voiceProfile);

        public static bool TryGetOwner(IVoiceProfile profile, out ReferenceHub owner)
        {
            if (m_Profiles.TryGetKey(profile, out var netId))
            {
                if (ReferenceHub.TryGetHubNetID(netId, out owner))
                {
                    return owner != null;
                }
            }

            owner = null;
            return false;
        }

        public static bool TryGetAvailableProfile(ReferenceHub hub, out IVoiceProfile profile)
        {
            if (m_ProfilePrefabs.TryGetFirst(prefab => prefab.IsAvailable(hub), out var result))
            {
                profile = result.Clone(hub);
                return profile != null;
            }

            profile = null;
            return false;
        }

        public static void SetProfile(ReferenceHub hub, IVoiceProfile profile)
            => m_Profiles[hub.netId] = profile;

        public static bool CanHearSelf(ReferenceHub hub)
        {
            if (m_Playback.Contains(hub.netId))
                return true;

            if (TryGetProfile(hub, out var profile)
                && profile is ScpVoiceProfile voiceProfile)
                return voiceProfile.AllowSelfHearing;

            return false;
        }

        public static bool CanBeHandled(VoiceChatChannel channel)
            => channel != VoiceChatChannel.None
            && channel != VoiceChatChannel.Intercom && channel != VoiceChatChannel.Spectator
            && channel != VoiceChatChannel.Scp1576 && channel != VoiceChatChannel.Mimicry
            && channel != VoiceChatChannel.Proximity && channel != VoiceChatChannel.Radio
            && channel != VoiceChatChannel.RoundSummary;

        private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
        {
            if (TryGetAvailableProfile(hub, out var profile))
            {
                SetProfile(hub, profile);
            }
        }

        private static void OnRoundRestart()
        {
            _isRestarting = true;

            m_Profiles.Clear();
            m_OvFlags.Clear();
            m_Playback.Clear();

            StaffFlags = StaffVoiceFlags.None;
            PriorityVoice = null;
        }

        private static void OnWaiting()
        {
            _isRestarting = false;
        }

        private static void RegisterEvents()
        {
            Reflection.TryAddHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);

            ServerEventType.RoundRestart.AddHandler<Action>(OnRoundRestart);
            ServerEventType.WaitingForPlayers.AddHandler<Action>(OnWaiting);

            FLog.Debug($"Registered events.");
        }

        private static void UnregisterEvents()
        {
            Reflection.TryRemoveHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);

            ServerEventType.RoundRestart.RemoveHandler<Action>(OnRoundRestart);
            ServerEventType.WaitingForPlayers.RemoveHandler<Action>(OnWaiting);

            FLog.Debug($"Unregistered events.");
        }
    }
}