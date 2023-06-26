using Compendium.Extensions;

namespace Compendium.Helpers.Overlay
{
    public static class OverlayExtensions
    {
        public static void ShowMessage(this ReferenceHub hub, object message, float duration = 2f, bool isPriority = false)
        {
            if (hub.TryGetController(out var controller))
            {
                if (controller.TryGetState<OverlayController>(out var overlay))
                {
                    overlay.Message(message, duration, isPriority);
                }
            }
        }

        public static void AddOverlayPart(this ReferenceHub hub, OverlayPart part)
        {
            if (hub.TryGetController(out var controller))
            {
                if (controller.TryGetState<OverlayController>(out var overlay))
                {
                    overlay.AddPart(part);
                }
            }
        }
    }
}