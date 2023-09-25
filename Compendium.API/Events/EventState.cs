using helpers.Pooling;

namespace Compendium.Events
{
    public class EventState : Poolable
    {
        public EventState()
            => Continue = new ValueReference();

        public int Index { get; set; } = 0;
        public bool IsComplete { get; set; }

        public ValueReference Continue { get; }

        public override void OnUnpooled()
        {
            base.OnUnpooled();

            Index = 0;
            IsComplete = false;
            Continue.Value = true;
        }
    }
}