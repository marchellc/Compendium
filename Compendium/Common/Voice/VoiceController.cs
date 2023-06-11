using Compendium.Input;
using Compendium.State;
using Compendium.State.Base;

using helpers.Extensions;
using helpers.Translations;
using PlayerRoles;
using PlayerRoles.Spectating;
using PlayerRoles.Voice;

using System.Collections.Generic;

using UnityEngine;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Common.Voice
{
    public class VoiceController : RequiredStateBase
    {
        private IVoiceChannel m_ReturnChannel;
        private IVoiceChannel m_Channel;

        private List<VoiceOverrides> m_Overrides = new List<VoiceOverrides>();

        public override StateFlags Flags => StateFlags.DisableUpdate;
        public override string Name => "Voice";

        public List<VoiceOverrides> Overrides => m_Overrides;

        public IVoiceChannel VoiceChannel => m_Channel;

        public override void OnLoaded()
        {
            InputManager.AddGlobalHandler(KeyCode.V, ProximityKey);
            InputManager.AddGlobalHandler(KeyCode.RightAlt, SwitchKey);
        }

        public override void HandlePlayerSpawn(RoleTypeId newRole)
        {
            if (m_Channel != null)
                Leave();

            if (newRole.GetTeam() is Team.SCPs)
                Join(StaticChannels.ScpChannel);
            else if (newRole.GetFaction() is Faction.FoundationStaff || newRole.GetFaction() is Faction.FoundationEnemy)
                Join(StaticChannels.ProximityChannel);
        }

        public bool Receive(IVoiceRole speakerRole, VoiceMessage voiceMessage, VoiceChatChannel sendChannel)
        {
            ReferenceHub.AllHubs.ForEach(receiver =>
            {
                if (receiver.netId == Player.netId)
                {
                    if (Overrides.Contains(VoiceOverrides.Playback))
                    {
                        voiceMessage.Channel = sendChannel;
                        speakerRole.VoiceModule.CurrentChannel = sendChannel;
                        Player.connectionToClient.Send(voiceMessage);
                    }
                    else
                    {
                        return;
                    }
                }

                if (Player.IsSCP())
                {
                    if (Player.IsSpectatedBy(receiver) && receiver.GetRoleId() == RoleTypeId.Overwatch)
                    {
                        voiceMessage.Channel = VoiceChatChannel.ScpChat;
                        speakerRole.VoiceModule.CurrentChannel = VoiceChatChannel.ScpChat;
                        receiver.connectionToClient.Send(voiceMessage);
                        return;
                    }
                }

                if (m_Channel != null)
                {
                    if (m_Channel.CanReceive(speakerRole, receiver) 
                        && m_Channel.Contains(receiver)
                        && m_Channel.Contains(speakerRole.VoiceModule.Owner))
                    {
                        speakerRole.VoiceModule.CurrentChannel = m_Channel.Channel;
                        voiceMessage.Channel = m_Channel.Channel;

                        m_Channel.Receive(speakerRole, receiver, voiceMessage);                        
                        return;
                    }
                }

                if (!(receiver.roleManager.CurrentRole is IVoiceRole recvRole))
                    return;

                var recvChannel = recvRole.VoiceModule.ValidateReceive(Player, sendChannel);
                if (recvChannel != VoiceChatChannel.None)
                {
                    voiceMessage.Channel = recvChannel;
                    speakerRole.VoiceModule.CurrentChannel = recvChannel;
                    receiver.connectionToClient.Send(voiceMessage);
                }
            });

            return true;
        }

        public void Join(IVoiceChannel voiceChannel)
        {
            if (m_Channel != null)
                Leave();

            m_Channel = voiceChannel;
            m_Channel.Join(Player);
        }

        public void Leave()
        {
            if (m_Channel != null)
            {
                m_Channel.Leave(Player);
                m_Channel = null;
            }

            if (m_ReturnChannel != null)
            {
                m_ReturnChannel = null;
                Join(m_ReturnChannel);
            }
        }

        public void ProximityKey(KeyCode key, ReferenceHub player, InputManager input)
        {
            if (player != Player)
                return;

            if (m_Channel == StaticChannels.ProximityChannel)
                HandlePlayerSpawn(Player.GetRoleId());
            else
                Join(StaticChannels.ProximityChannel);
        }

        public void SwitchKey(KeyCode key, ReferenceHub player, InputManager input)
        {
            if (player != Player)
                return;

            if (m_Channel != null)
            {
                if (m_Channel == StaticChannels.ScpChannel)
                {
                    Join(StaticChannels.ProximityChannel);
                    OnChannelChanged();
                }
                else
                {
                    Join(StaticChannels.ScpChannel);
                    OnChannelChanged();
                }
            }
            else
            {
                Join(StaticChannels.ProximityChannel);
                OnChannelChanged();
            }
        }

        private void OnChannelChanged()
        {
            var text = Translator.Get("voice.notify", m_Channel.Name);
        }
    }
}