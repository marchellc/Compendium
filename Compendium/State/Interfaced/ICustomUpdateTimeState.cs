namespace Compendium.State.Interfaced
{
    public interface ICustomUpdateTimeState : IState
    {
        float UpdateInterval { get; }
    }
}