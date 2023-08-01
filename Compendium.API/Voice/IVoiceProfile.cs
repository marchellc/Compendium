namespace Compendium.Voice
{
    public interface IVoiceProfile
    {
        ReferenceHub Owner { get; }

        void Process(VoicePacket packet);
    }
}