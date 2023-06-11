using Compendium.State;

using PlayerRoles.Voice;

using System.Collections.Generic;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Common.Voice.Channels
{
    public class VoiceChannelBase : IVoiceChannel
    {
        private readonly HashSet<ReferenceHub> m_Members = new HashSet<ReferenceHub>();

        public virtual string Name => "Voice Channel Base";
        public virtual int Id => -1;

        public virtual VoiceChatChannel Channel => VoiceChatChannel.Proximity;

        public IReadOnlyCollection<ReferenceHub> Members => m_Members;

        public bool Contains(ReferenceHub hub) => m_Members.Contains(hub);
        public virtual bool CanJoin(ReferenceHub hub) => true;
        public virtual bool CanReceive(IVoiceRole speakerRole, ReferenceHub receiver)
        {
            if (speakerRole.VoiceModule.Owner.TryGetState<VoiceController>(out var vc))
            {
                if (vc.VoiceChannel != null && vc.VoiceChannel.Id == Id)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public virtual void Join(ReferenceHub hub)
        {
            m_Members.Add(hub);
        }

        public virtual void Leave(ReferenceHub hub)
        {
            m_Members.Remove(hub);
        }

        public virtual void Receive(IVoiceRole speakerRole, ReferenceHub receiver, VoiceMessage voiceMessage) => receiver.connectionToClient.Send(voiceMessage);
    }
}