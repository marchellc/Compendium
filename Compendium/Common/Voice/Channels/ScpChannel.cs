using PlayerRoles;
using PlayerRoles.Voice;

using VoiceChat;

namespace Compendium.Common.Voice.Channels
{
    public class ScpChannel : VoiceChannelBase
    {
        public override VoiceChatChannel Channel => VoiceChatChannel.ScpChat;
        public override int Id => 50;
        public override string Name => "Proximity";

        public override bool CanReceive(IVoiceRole speakerRole, ReferenceHub receiver)
        {
            if (!base.CanReceive(speakerRole, receiver))
                return false;

            if (!receiver.IsSCP())
                return false;

            return true;
        }
    }
}
