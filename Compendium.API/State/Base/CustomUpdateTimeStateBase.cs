using Compendium.State.Interfaced;

namespace Compendium.State.Base
{
    public class CustomUpdateTimeStateBase : StateBase, ICustomUpdateTimeState
    {
        public virtual float UpdateInterval { get; }
    }
}