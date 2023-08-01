using PlayerRoles;

using System;

namespace Compendium.Voice
{
    public interface IVoicePrefab
    {
        RoleTypeId[] Roles { get; }

        Type Type { get; }

        IVoiceProfile Instantiate(ReferenceHub owner);
    }
}