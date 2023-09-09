namespace Compendium.Voice
{
    public interface IVoiceProfile
    {
        ReferenceHub Owner { get; }

        bool IsEnabled { get; }

        void Process(VoicePacket packet);

        void Enable();
        void Disable();
    }
}