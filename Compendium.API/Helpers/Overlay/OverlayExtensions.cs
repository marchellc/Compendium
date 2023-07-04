using Compendium.Extensions;

using Hints;

namespace Compendium.Helpers.Overlay
{
    public static class OverlayExtensions
    {
        public static void ShowMessage(this ReferenceHub hub, object message, float duration = 2f, bool isPriority = false)
        {
            hub.hints.Show(new TextHint(message.ToString(), new HintParameter[] { new StringHintParameter(message.ToString()) }, null, duration + 0.2f));

            /*
            if (hub.TryGetController(out var controller))
            {
                if (controller.TryGetState<OverlayController>(out var overlay))
                {
                    overlay.Message(message, duration, isPriority);
                }
            }
            */
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