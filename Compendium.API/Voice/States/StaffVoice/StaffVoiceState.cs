using helpers.Enums;
using helpers;

using VoiceChat;

namespace Compendium.Voice.States.StaffVoice
{
    public class StaffVoiceState : IVoiceChatState
    {
        private ReferenceHub _startedBy;

        public StaffVoiceState(ReferenceHub starter)
            => _startedBy = starter;

        public ReferenceHub Starter => _startedBy;
        public StaffVoiceFlag Flag { get; set; } = StaffVoiceFlag.StaffOnly;

        public bool Process(VoicePacket packet)
        {
            if (Starter is null)
                return false;

            packet.Destinations.ForEach(p =>
            {
                var receiver = p.Key;

                if (receiver.netId == Starter.netId || receiver.netId == packet.Speaker.netId)
                    return;

                if (Flag is StaffVoiceFlag.StaffOnly)
                {
                    if (!packet.Speaker.IsStaff())
                    {
                        if (Flag.HasFlagFast(StaffVoiceFlag.PlayersHearPlayers))
                        {
                            if (receiver.IsStaff())
                            {
                                packet.Destinations[receiver] = VoiceChatChannel.None;
                            }
                        }
                    } 
                    else
                    {
                        if (Flag.HasFlagFast(StaffVoiceFlag.PlayersHearStaff) && !receiver.IsStaff())
                        {
                            packet.Destinations[receiver] = VoiceChatChannel.RoundSummary;
                        }
                        else
                        {
                            if (receiver.IsStaff())
                            {
                                packet.Destinations[receiver] = VoiceChatChannel.RoundSummary;
                            }
                            else
                            {
                                packet.Destinations[receiver] = VoiceChatChannel.None;
                            }
                        }
                    }
                }
            });

            return true;
        }
    }
}