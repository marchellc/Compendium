namespace Compendium.Items.Firearms
{
    public class CustomFirearmProperties
    {
        public ItemType AmmoType { get; set; } = ItemType.None;

        public byte MaxAmmo { get; set; } = 30;
        public byte StartAmmo { get; set; } = 30;
    }
}