using Compendium.Helpers.Staff;

using PlayerRoles.Spectating;
using PlayerRoles.Voice;

using System.Collections.Generic;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Common.Voice.Channels
{
    public class CustomVoiceChannel : ICustomVoiceChannel
    {
        private ReferenceHub m_Owner;

        private HashSet<ReferenceHub> m_Members = new HashSet<ReferenceHub>();
        private HashSet<string> m_Permits = new HashSet<string>();

        private string m_Name;

        private int m_Id;

        private VoiceChatChannel m_Channel;

        public ReferenceHub Owner => m_Owner;

        public string Name => m_Name;

        public int Id => m_Id;

        public VoiceChatChannel Channel => m_Channel;

        public IReadOnlyCollection<ReferenceHub> Members => m_Members;

        public bool CanJoin(ReferenceHub hub)
        {
            if (StaffHelper.IsConsideredStaff(hub))
                return true;

            if (!m_Permits.Contains(hub.characterClassManager.UserId))
                return false;

            return false;
        }

        public bool CanReceive(IVoiceRole speakerRole, ReferenceHub receiver)
        {
            if (speakerRole.VoiceModule.Owner.IsSpectatedBy(receiver) && StaffHelper.IsConsideredStaff(receiver))
                return true;

            if (!m_Members.Contains(receiver))
                return false;

            return true;
        }

        public bool Contains(ReferenceHub hub) => m_Members.Contains(hub);

        public void Join(ReferenceHub hub)
        {
            if (!CanJoin(hub))
                return;

            m_Members.Add(hub);
        }

        public void Leave(ReferenceHub hub) => m_Members.Remove(hub);

        public void Permit(ReferenceHub hub)
        {
            m_Permits.Add(hub.characterClassManager.UserId);
        }

        public void Receive(IVoiceRole speakerRole, ReferenceHub receiver, VoiceMessage voiceMessage) => receiver.connectionToClient.Send(voiceMessage);

        public void Remove(ReferenceHub hub)
        {
            m_Permits.Remove(hub.characterClassManager.UserId);
        }
    }
}