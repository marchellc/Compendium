using PlayerRoles.Voice;

using VoiceChat;

namespace Compendium.Voice.Profiles
{
    public class BaseProfile : IVoiceProfile
    {
        private ReferenceHub _owner;

        public BaseProfile(ReferenceHub owner)
            => _owner = owner;

        public ReferenceHub Owner => _owner;

        public IVoiceRole Role => _owner.Role() as IVoiceRole;
        public VoiceModuleBase Module => Role?.VoiceModule ?? null;
        public VoiceChatChannel Channel { get => Module?.CurrentChannel ?? VoiceChatChannel.None; set => Module.CurrentChannel = value; }

        public virtual void Process(VoicePacket packet) { }
    }
}