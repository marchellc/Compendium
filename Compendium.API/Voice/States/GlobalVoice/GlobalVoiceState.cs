using helpers.Enums;
using helpers.Extensions;

using VoiceChat;

namespace Compendium.Voice.States
{
    public class GlobalVoiceState : IVoiceChatState
    {
        private ReferenceHub _startedBy;

        public GlobalVoiceState(ReferenceHub starter)
            => _startedBy = starter;

        public ReferenceHub Starter => _startedBy;
        public GlobalVoiceFlag GlobalVoiceFlag { get; set; } = GlobalVoiceFlag.SpeakerOnly;

        public bool Process(VoicePacket packet)
        {
            if (Starter is null)
                return false;

            packet.Destinations.ForEach(p =>
            {
                var receiver = p.Key;

                if (receiver.netId == Starter.netId || receiver.netId == packet.Speaker.netId)
                    return;

                if (packet.Speaker.netId != Starter.netId)
                {
                    if (GlobalVoiceFlag is GlobalVoiceFlag.SpeakerOnly)
                    {
                        packet.Destinations[receiver] = VoiceChatChannel.None;
                        return;
                    }

                    if (GlobalVoiceFlag is GlobalVoiceFlag.StaffOnly && packet.Speaker.serverRoles.Staff)
                    {
                        packet.Destinations[receiver] = VoiceChatChannel.RoundSummary;
                        return;
                    }

                    if (GlobalVoiceFlag.HasFlagFast(GlobalVoiceFlag.PlayerVoice) && !packet.Speaker.serverRoles.Staff && !receiver.serverRoles.Staff)
                    {
                        packet.Destinations[receiver] = VoiceChatChannel.RoundSummary;
                        return;
                    }

                    packet.Destinations[receiver] = VoiceChatChannel.None;
                }
                else
                {
                    packet.Destinations[receiver] = VoiceChatChannel.RoundSummary;
                }
            });

            return true;
        }
    }
}