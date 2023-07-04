using Compendium.Extensions;
using Compendium.Helpers.Overlay;
using Compendium.Input;

using PlayerRoles;
using PlayerRoles.Spectating;

using UnityEngine;

using VoiceChat.Networking;

namespace Compendium.Voice.Profiles
{
    public class ScpVoiceProfile : VoiceProfileBase
    {
        private static bool m_ProximityHandler;

        public override string Name => "SCP Chat";

        public bool IsProximityActive { get; set; }
        public bool AllowSelfHearing { get; set; }

        public ScpVoiceProfile(ReferenceHub owner) : base(owner)
        {
            if (!m_ProximityHandler)
                m_ProximityHandler = InputHandler.TryAddHandler("voice_proximity", KeyCode.RightAlt, ProximityKey);
        }

        public override void HandleSpeaker(VoiceMessage message)
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (hub.netId == Owner.netId && !AllowSelfHearing)
                    continue;

                if (!hub.IsSCP(true))
                {
                    if (IsProximityActive && hub.IsAlive())
                    {
                        if (hub.IsWithinDistance(Owner.transform.position, VoiceConfigs.ScpProximityDistance))
                        {
                            message.Channel = VoiceChat.VoiceChatChannel.Proximity;
                            SetCurrentChannel(VoiceChat.VoiceChatChannel.Proximity);
                            hub.connectionToClient.Send(message);
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (Owner.IsSpectatedBy(hub) && hub.GetRoleId() is RoleTypeId.Overwatch && VoiceConfigs.AllowOverwatchScpChat)
                        {
                            message.Channel = VoiceChat.VoiceChatChannel.RoundSummary;

                            SetCurrentChannel(VoiceChat.VoiceChatChannel.RoundSummary);

                            hub.connectionToClient.Send(message);
                            hub.ShowMessage($"\n\n<b><color=#33FFA5>Overwatch allows you to listen to SCP players.</color></b>", 1f, true);

                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                message.Channel = VoiceChat.VoiceChatChannel.ScpChat;
                SetCurrentChannel(VoiceChat.VoiceChatChannel.ScpChat);
                hub.connectionToClient.Send(message);
            }
        }

        public bool IsProximityAvailable()
            => VoiceConfigs.ProximityScps.Contains(Owner.GetRoleId());

        public void SwitchProximity()
        {
            if (!IsProximityAvailable())
            {
                IsProximityActive = false;
                Owner.ShowMessage($"<b><color=#33FFA5><color=#FF0000>Proximity chat is not available</color> for your role.</color></b>", 3f, true);
                return;
            }

            IsProximityActive = !IsProximityActive;

            if (IsProximityActive) 
                Owner.ShowMessage($"\n\n<b><color=#33FFA5>Switched to</color> <color=#FF0000>proximity</color> <color=#33FFA5>voice chat</color>.</b>", 3f, true);
            else 
                Owner.ShowMessage($"\n\n<b><color=#33FFA5>Switched to regular</color> <color=#FF0000>SCP chat</color>.</b>", 3f, true);
        }

        private static void ProximityKey(ReferenceHub hub)
        {
            if (VoiceController.TryGetProfile(hub, out var vcProfile) 
                && vcProfile is ScpVoiceProfile scpProfile)
            {
                scpProfile.SwitchProximity();
            }
        }
    }
}