namespace Compendium.State.Interfaced
{
    public interface ICustomRangedUpdateTimeState : ICustomUpdateTimeState
    {
        float MaxInterval { get; }
        float MinInterval { get; }
    }
}