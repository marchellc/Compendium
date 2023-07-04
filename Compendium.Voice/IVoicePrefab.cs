namespace Compendium.Voice
{
    public interface IVoicePrefab
    {
        bool IsAvailable(ReferenceHub hub);

        IVoiceProfile Clone(ReferenceHub target);
    }
}