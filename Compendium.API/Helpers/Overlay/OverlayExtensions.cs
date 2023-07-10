using Compendium.Extensions;

using Hints;

namespace Compendium.Helpers.Overlay
{
    public static class OverlayExtensions
    {
        public static void ShowMessage(this ReferenceHub hub, object message, float duration = 2f, byte priority = 0)
        {
            hub.hints.Show(new TextHint(message.ToString(), new HintParameter[] { new StringHintParameter(message.ToString()) }, null, duration + 0.01f));
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