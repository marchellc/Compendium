namespace Compendium.Common.Voice
{
    public interface ICustomVoiceChannel : IVoiceChannel
    {
        ReferenceHub Owner { get; }
        
        void Permit(ReferenceHub hub);
        void Remove(ReferenceHub hub);
    }
}