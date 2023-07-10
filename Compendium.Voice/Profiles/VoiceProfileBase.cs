using PlayerRoles.Voice;

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
    }
}