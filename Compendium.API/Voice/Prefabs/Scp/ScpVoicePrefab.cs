using System;

using Compendium.Voice.Profiles.Scp;

using PlayerRoles;

namespace Compendium.Voice.Prefabs.Scp
{
    public class ScpVoicePrefab : BasePrefab
    {
        public ScpVoicePrefab() : base(RoleTypeId.Scp049, RoleTypeId.Scp0492, RoleTypeId.Scp079, RoleTypeId.Scp096, RoleTypeId.Scp106, RoleTypeId.Scp173, RoleTypeId.Scp939) { }

        public override Type Type { get; } = typeof(ScpVoiceProfile);

        public override IVoiceProfile Instantiate(ReferenceHub owner)
            => new ScpVoiceProfile(owner);
    }
}