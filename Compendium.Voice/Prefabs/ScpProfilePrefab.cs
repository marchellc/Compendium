using Compendium.Voice.Profiles;

using PlayerRoles;

namespace Compendium.Voice.Prefabs
{
    public class ScpProfilePrefab : IVoicePrefab
    {
        public IVoiceProfile Clone(ReferenceHub target) => new ScpVoiceProfile(target);

        public bool IsAvailable(ReferenceHub hub) => hub.IsSCP(true);
    }
}
