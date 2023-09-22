using InventorySystem.Items;
using InventorySystem.Items.Firearms;

using System.Collections.Generic;

namespace Compendium.Custom.Items
{
    public static class CustomItemOverrides
    {
        private static Dictionary<Firearm, bool> _disarmOverride = new Dictionary<Firearm, bool>();
        private static Dictionary<ItemBase, float> _weightOverride = new Dictionary<ItemBase, float>();

        public static void SetWeightOverride(ItemBase item, float value, bool remove)
        {
            if (item is null)
                return;

            if (remove)
            {
                _weightOverride.Remove(item);
                return;
            }

            _weightOverride[item] = value;
        }

        public static void SetDisarmOverride(Firearm firearm, bool value, bool remove)
        {
            if (firearm is null)
                return;

            if (remove)
            {
                _disarmOverride.Remove(firearm);
                return;
            }

            _disarmOverride[firearm] = value;
        }
    }
}
