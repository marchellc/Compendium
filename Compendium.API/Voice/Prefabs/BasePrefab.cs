using PlayerRoles;

using System;

namespace Compendium.Voice.Prefabs
{
    public class BasePrefab : IVoicePrefab
    {
        private RoleTypeId[] _roles;

        public BasePrefab(params RoleTypeId[] roles)
            => _roles = roles;

        public RoleTypeId[] Roles => _roles;

        public virtual Type Type { get; }
        public virtual IVoiceProfile Instantiate(ReferenceHub owner) => null;
    }
}