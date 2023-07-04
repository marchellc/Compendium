using PlayerRoles.Voice;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice.Profiles
{
    public class VoiceProfileBase : IVoiceProfile
    {
        private readonly ReferenceHub m_Owner;

        public ReferenceHub Owner => m_Owner;
        public IVoiceRole Role => Owner.roleManager.CurrentRole as IVoiceRole;
        public VoiceModuleBase Module => Role?.VoiceModule;

        public virtual string Name => null;

        public VoiceProfileBase(ReferenceHub owner)
            => m_Owner = owner;

        public virtual void HandleSpeaker(VoiceMessage message) { }

        public void SetCurrentChannel(VoiceChatChannel channel)
        {
            if (Module != null)
            {
                Module.CurrentChannel = channel;
            }
        }
    }
}