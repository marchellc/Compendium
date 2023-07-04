using Compendium.Features;
using Compendium.Helpers.Events;
using Compendium.Voice.Prefabs;

using helpers;
using helpers.Extensions;

using PlayerRoles;
using PlayerRoles.Voice;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice
{
    public static class VoiceController
    {
        private static readonly Dictionary<uint, IVoiceProfile> m_Profiles = new Dictionary<uint, IVoiceProfile>();
        private static readonly HashSet<IVoicePrefab> m_ProfilePrefabs = new HashSet<IVoicePrefab>()
        {
            new ScpProfilePrefab()
        };

        public static bool IsActive { get; private set; }

        public static GlobalVoiceFlags GlobalVoiceFlags { get; set; } = GlobalVoiceFlags.None;
        public static ReferenceHub GlobalSpeaker { get; set; }

        public static void Load()
        {
            if (IsActive)
            {
                FLog.Warn($"The Voice Controller is already active!");
                return;
            }

            IsActive = true;

            VoicePatch.Apply();

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

            VoicePatch.Unapply();

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
        {
            m_Profiles[hub.netId] = profile;
            FLog.Debug($"Activated voice profile {profile.Name} for user: {hub.LoggedNameFromRefHub()}");
        }

        public static bool CanBeHandled(VoiceChatChannel channel)
            => channel != VoiceChatChannel.None
            && channel != VoiceChatChannel.Intercom && channel != VoiceChatChannel.Spectator
            && channel != VoiceChatChannel.Scp1576 && channel != VoiceChatChannel.Mimicry
            && channel != VoiceChatChannel.Proximity && channel != VoiceChatChannel.Radio
            && channel != VoiceChatChannel.RoundSummary;

        public static void HandleMessage(ReferenceHub speaker, IVoiceRole speakerRole, VoiceMessage message)
        {
            if (TryGetProfile(speaker, out var profile))
            {
                profile.HandleSpeaker(message);
            }
            else
            {
                FLog.Error($"Missing voice profile: {speaker.LoggedNameFromRefHub()} ({speaker.GetRoleId()})");
            }
        }

        private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
        {
            if (TryGetAvailableProfile(hub, out var profile))
            {
                SetProfile(hub, profile);
            }
        }

        private static void OnRoundRestart()
        {
            m_Profiles.Clear();
            GlobalVoiceFlags = GlobalVoiceFlags.None;
            FLog.Debug($"Cleared round-temporary variables.");
        }

        private static void RegisterEvents()
        {
            Reflection.TryAddHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);
            ServerEventType.RoundRestart.AddHandler<Action>(OnRoundRestart);
            FLog.Debug($"Registered events.");
        }

        private static void UnregisterEvents()
        {
            Reflection.TryRemoveHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);
            ServerEventType.RoundRestart.RemoveHandler<Action>(OnRoundRestart);
            FLog.Debug($"Unregistered events.");
        }
    }
}