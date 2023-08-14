namespace Compendium.EasyComponents
{
    public class RangedTickRateEasyComponent : EasyComponent
    {
        public virtual float MaxTickRate { get; } = 1f;
        public virtual float MinTickRate { get; } = 0.01f;

        public override float TickRate => (MinTickRate + UnityEngine.Random.Range(MinTickRate, MaxTickRate) / (MaxTickRate + (MaxTickRate - MinTickRate)));
    }
}