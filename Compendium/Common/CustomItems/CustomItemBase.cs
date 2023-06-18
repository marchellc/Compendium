using Compendium.Helpers.Hints;
using Compendium.State;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

namespace Compendium.Common.CustomItems
{
    public class CustomItemBase : ICustomItem
    {
        private readonly string m_Name;
        private readonly int m_Id;
        private readonly ItemType m_Type;

        public CustomItemBase(string name, int id, ItemType type, bool useDisplay = true)
        {
            m_Name = name;
            m_Id = id;
            m_Type = type;
            m_Status = CustomItemStatus.Prefab;

            if (useDisplay)
            {
                m_Display = new CustomItemDisplay(this);
            }
        }

        private bool m_IsSelected;

        private CustomItemStatus m_Status;

        private ItemBase m_Item;
        private ItemPickupBase m_Pickup;
        private ReferenceHub m_Owner;

        private CustomItemDisplay m_Display;

        public string Name => m_Name;
        public int Id => m_Id;
        public bool IsSelected => m_IsSelected;

        public ItemType Type => m_Type;
        public CustomItemStatus Status => m_Status;

        public ItemBase Item { get => m_Item; set => m_Item = value; }
        public ItemPickupBase Pickup { get => m_Pickup; set => m_Pickup = value; }
        public ReferenceHub Owner { get => m_Owner; set => m_Owner = value; }

        public virtual ICustomItem Instantiate() => null;

        public virtual void OnDeselected()
        {
            m_IsSelected = false;

            if (m_Display != null)
            {
                if (m_Owner.TryGetState<HintController>(out var hints))
                {
                    hints.Override = null;
                }
            }
        }

        public virtual void OnDeselecting()
        {

        }

        public virtual void OnSelected()
        {
            m_IsSelected = true;

            if (m_Display != null)
            {
                if (m_Owner.TryGetState<HintController>(out var hints))
                {
                    hints.Override = m_Display.Hint;
                }
            }
        }

        public virtual void OnSelecting()
        {

        }

        public virtual bool CanAdd(ReferenceHub target) => true;
        public virtual bool CanRemove() => true;

        public virtual void OnAdded()
        {

        }

        public virtual void OnRemoved()
        {

        }
    }
}
