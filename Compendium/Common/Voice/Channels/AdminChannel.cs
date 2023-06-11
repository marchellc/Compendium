using Compendium.Helpers.Staff;

using PlayerRoles.Voice;

using VoiceChat;

namespace Compendium.Common.Voice.Channels
{
    public class AdminChannel : VoiceChannelBase
    {
        public override VoiceChatChannel Channel => VoiceChatChannel.Spectator;
        public override int Id => 40;
        public override string Name => "Admin-Only";

        public override bool CanJoin(ReferenceHub hub) => StaffHelper.IsConsideredStaff(hub);
        public override bool CanReceive(IVoiceRole speakerRole, ReferenceHub receiver)
        {
            if (!base.CanReceive(speakerRole, receiver))
                return false;

            if (!StaffHelper.IsConsideredStaff(receiver))
                return false;

            return true;
        }
    }
}
