using System;

using Compendium.Voice.Profiles.Scp;

using PlayerRoles;

namespace Compendium.Voice.Prefabs.Scp
{
    public class ScpVoicePrefab : BasePrefab
    {
        public ScpVoicePrefab() : base(Plugin.Config.VoiceSettings.AllowedScpChat) { }

        public override Type Type { get; } = typeof(ScpVoiceProfile);

        public override IVoiceProfile Instantiate(ReferenceHub owner)
            => new ScpVoiceProfile(owner);
    }
}