namespace Compendium.Voice
{
    public interface IVoiceProfile
    {
        string Name { get; }
        ReferenceHub Owner { get; }
    }
}