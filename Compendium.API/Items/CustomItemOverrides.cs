using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Pickups;

using System.Collections.Generic;

namespace Compendium.Items
{
    public static class CustomItemOverrides
    {
        private static Dictionary<Firearm, bool> _disarmOverride = new Dictionary<Firearm, bool>();
        private static Dictionary<Firearm, float> _penetrationOverride = new Dictionary<Firearm, float>();
        private static Dictionary<Firearm, float> _lengthOverride = new Dictionary<Firearm, float>();
        private static Dictionary<Firearm, byte> _maxAmmoOverride = new Dictionary<Firearm, byte>();

        private static Dictionary<ItemBase, float> _weightOverride = new Dictionary<ItemBase, float>();

        private static Dictionary<ItemPickupBase, float> _searchTimeOverrides = new Dictionary<ItemPickupBase, float>();
        private static Dictionary<ItemPickupBase, bool> _frozenOverrides = new Dictionary<ItemPickupBase, bool>();

        public static void SetMaxAmmoOverride(Firearm item, byte value, bool remove)
        {
            if (item is null)
                return;

            if (remove)
            {
                _maxAmmoOverride.Remove(item);
                return;
            }

            _maxAmmoOverride[item] = value;
        }

        public static void SetLengthOverride(Firearm item, float value, bool remove)
        {
            if (item is null)
                return;

            if (remove)
            {
                _lengthOverride.Remove(item);
                return;
            }

            _lengthOverride[item] = value;
        }

        public static void SetPenetrationOverride(Firearm item, float value, bool remove)
        {
            if (item is null)
                return;

            if (remove)
            {
                _penetrationOverride.Remove(item);
                return;
            }

            _penetrationOverride[item] = value;
        }

        // StandardPhysicsModule.ServerWriteRigidbody
        public static void SetFrozenOverride(ItemPickupBase item, bool value, bool remove)
        {
            if (item is null)
                return;

            if (remove)
            {
                _frozenOverrides.Remove(item);
                return;
            }

            _frozenOverrides[item] = value;
        }

        public static void SetSearchTimeOverride(ItemPickupBase item, float value, bool remove)
        {
            if (item is null)
                return;

            if (remove)
            {
                _searchTimeOverrides.Remove(item);
                return;
            }

            _searchTimeOverrides[item] = value;
        }

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
