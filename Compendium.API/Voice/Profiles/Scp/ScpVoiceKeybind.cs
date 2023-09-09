using Compendium.Input;

using UnityEngine;

namespace Compendium.Voice.Profiles.Scp
{
    public class ScpVoiceKeybind : IInputHandler
    {
        public KeyCode Key => KeyCode.RightAlt;

        public bool IsChangeable => true;

        public string Id => "voice_proximity";

        public void OnPressed(ReferenceHub player)
        {
            var profile = VoiceChat.GetProfile(player);

            if (profile != null && profile is ScpVoiceProfile scpProfile)
                scpProfile.OnSwitchUsed();
        }
    }
}