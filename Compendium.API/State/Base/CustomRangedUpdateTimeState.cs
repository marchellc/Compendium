using Compendium.State.Interfaced;

namespace Compendium.State.Base
{
    public class CustomRangedUpdateTimeState : CustomUpdateTimeStateBase, ICustomRangedUpdateTimeState
    {
        public float MaxInterval { get; }
        public float MinInterval { get; }

        public override float UpdateInterval => UnityEngine.Random.Range(MinInterval, MaxInterval);
    }
}