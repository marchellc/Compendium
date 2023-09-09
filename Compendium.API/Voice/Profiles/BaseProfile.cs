using PlayerRoles.Voice;

using VoiceChat;

namespace Compendium.Voice.Profiles
{
    public class BaseProfile : IVoiceProfile
    {
        private ReferenceHub _owner;
        private bool _isEnabled;

        public BaseProfile(ReferenceHub owner)
            => _owner = owner;

        public ReferenceHub Owner => _owner;

        public bool IsEnabled => _isEnabled;

        public IVoiceRole Role => _owner.Role() as IVoiceRole;
        public VoiceModuleBase Module => Role?.VoiceModule ?? null;
        public VoiceChatChannel Channel { get => Module?.CurrentChannel ?? VoiceChatChannel.None; set => Module.CurrentChannel = value; }

        public virtual void Process(VoicePacket packet) { }

        public virtual void Disable()
            => _isEnabled = false;

        public virtual void Enable()
            => _isEnabled = true;
    }
}