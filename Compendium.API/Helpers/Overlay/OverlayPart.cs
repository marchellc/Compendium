using System;

namespace Compendium.Helpers.Overlay
{
    public class OverlayPart
    {
        public OverlayPosition Position { get; }
        public OverlayElementType ElementType { get; }

        public Func<string> Data { get; }

        public float? Duration { get; }

        public bool IsPriority { get; }

        public OverlayPart(object message, float duration, bool isPriority = false)
        {
            Position = OverlayPosition.Center;
            ElementType = OverlayElementType.Message;

            Data = () => message?.ToString() ?? "null message";
            Duration = duration;
            IsPriority = isPriority;
        }

        public OverlayPart(OverlayPosition position, Func<string> data)
        {
            Position = position;
            Data = data;

            ElementType = OverlayElementType.HudPart;
            Duration = null;
            IsPriority = false;
        }
    }
}