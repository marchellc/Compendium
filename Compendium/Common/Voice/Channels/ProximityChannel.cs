using PlayerRoles;
using PlayerRoles.Voice;
using UnityEngine;
using VoiceChat;

namespace Compendium.Common.Voice.Channels
{
    public class ProximityChannel : VoiceChannelBase
    {
        public override VoiceChatChannel Channel => VoiceChatChannel.Proximity;
        public override int Id => 35;
        public override string Name => "Proximity";

        public override bool CanJoin(ReferenceHub hub)
        {
            if (!base.CanJoin(hub))
                return false;

            if (!hub.IsSCP() && hub.IsAlive())
                return true;

            if (hub.IsSCP())
            {
                if (Plugin.Config.VoiceSettings.ScpsAllowedProximity.Contains(hub.GetRoleId()))
                    return true;
                else
                    return false;
            }

            return false;
        }

        public override bool CanReceive(IVoiceRole speakerRole, ReferenceHub receiver)
        {
            if (!base.CanReceive(speakerRole, receiver))
                return false;

            if (speakerRole.VoiceModule.Owner.IsSCP())
            {
                if (Vector3.Distance(speakerRole.VoiceModule.Owner.transform.position, receiver.transform.position) >= Plugin.Config.VoiceSettings.ProximityDistancing[speakerRole.VoiceModule.Owner.GetTeam()])
                    return false;
                else
                    return true;
            }
            else
            {
                if (!speakerRole.VoiceModule.Owner.IsAlive() && receiver.IsAlive())
                    return false;
                else if (speakerRole.VoiceModule.Owner.IsAlive() && !receiver.IsAlive())
                    return false;
                else
                    return true;
            }
        }
    }
}