namespace Compendium.Items.Firearms
{
    public class CustomFirearm<TItemHandler, TPickupHandler> : CustomItem<TItemHandler, TPickupHandler>
        where TItemHandler : CustomFirearmItemHandler, new()
        where TPickupHandler : CustomFirearmPickupHandler, new()
    {
        public virtual CustomFirearmProperties Properties { get; }
    }
}