using VoiceChat.Networking;

namespace Compendium.Voice
{
    public interface IVoiceProfile
    {
        string Name { get; }

        ReferenceHub Owner { get; }

        void HandleSpeaker(VoiceMessage message);
    }
}