using helpers;

namespace Compendium.Items
{
    public class CustomItem<TItemHandler, TPickupHandler> : CustomItemBase
        where TItemHandler : CustomItemHandlerBase, new()
        where TPickupHandler : CustomPickupHandlerBase, new()
    {
        internal override CustomItemHandlerBase CreateItemHandler(ushort serial)
        {
            var handler = Reflection.Instantiate<TItemHandler>();
            handler.Serial = serial;
            return handler;
        }

        internal override CustomPickupHandlerBase CreatePickupHandler(ushort serial)
        {
            var handler = Reflection.Instantiate<TPickupHandler>();
            handler.Serial = serial;
            return handler;
        }
    }
}