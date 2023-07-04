using Compendium.Input;

using UnityEngine;

namespace Compendium.Grab
{
    public static class GrabInput
    {
        public static void Load()
        {
            InputHandler.TryAddHandler("grab_add", KeyCode.Mouse2, GrabHandle);
            InputHandler.TryAddHandler("grab_remove", KeyCode.Backspace, UngrabHandler);
        }

        public static void Unload()
        {
            InputHandler.TryRemoveHandler("grab_add");
            InputHandler.TryRemoveHandler("grab_remove");
        }

        public static void GrabHandle(ReferenceHub hub)
        {
            if (GrabObserver.TryObserve(hub, out var target))
            {
                GrabHandler.Grab(hub, target);
            }
        }

        public static void UngrabHandler(ReferenceHub hub)
        {
            GrabHandler.Ungrab(hub);
        }
    }
}