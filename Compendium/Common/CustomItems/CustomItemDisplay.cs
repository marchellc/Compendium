using Compendium.Helpers.Hints;

namespace Compendium.Common.CustomItems
{
    public class CustomItemDisplay
    {
        private readonly ICustomItem m_Item;
        private readonly Hint m_Hint;

        public CustomItemDisplay(ICustomItem customItem)
        {
            m_Item = customItem;
            m_Hint = new HintBuilder()
                .WithDuration(1f)
                .WithUpdate(Update)
                .WithPriority(HintPriority.High)
                .Build();
        }

        public Hint Hint => m_Hint;

        public void Update(HintWriter writer)
        {
            writer.Clear();

            writer.EmitAlign(HintAlign.Right);
            writer.EmitPosition(100);

            writer.Emit($"<--> Selected Custom Item");
            writer.Emit($"<b><color=#ff0000>Item:</color></b> <i><color=green>{m_Item.Name}</color></i>");

            if (m_Item is ICustomWeapon weapon)
            {
                writer.Emit($"<b><color=#ff0000>Ammo:</color></b> <i>color=green>{weapon.CurAmmo}</color> / <color=#ff0000>{weapon.MaxAmmo}</color></i>");
            }
        }
    }
}