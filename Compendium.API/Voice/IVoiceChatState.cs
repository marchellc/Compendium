namespace Compendium.Voice
{
    public interface IVoiceChatState
    {
        ReferenceHub Starter { get; }

        bool Process(VoicePacket packet);
    }
}