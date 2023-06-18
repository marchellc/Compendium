using UnityEngine;

namespace Compendium.Common.CustomItems
{
    public class CustomWeaponBase : CustomItemBase, ICustomWeapon
    {
        private readonly ItemType m_AmmoType;

        public CustomWeaponBase(
            string name, 
            int id,
            
            ItemType type, 
            ItemType ammoType, 
            
            int ammoPerShot = 5, 
            int maxAmmo = int.MaxValue, 
            int startingAmmo = 60) : base(name, id, type)
        {
            m_AmmoType = ammoType;
            m_AmmoPerShot = ammoPerShot;
            m_MaxAmmo = maxAmmo;
            m_CurAmmo = startingAmmo;
        }

        private int m_MaxAmmo;
        private int m_CurAmmo;
        private int m_AmmoPerShot;

        public ItemType AmmoType => m_AmmoType;

        public int MaxAmmo { get => m_MaxAmmo; set => m_MaxAmmo = value; }
        public int CurAmmo { get => m_CurAmmo; set => m_CurAmmo = value; }
        public int AmmoPerShot { get => m_AmmoPerShot; set => m_AmmoPerShot = value; }

        public virtual bool OnInspecting() => true;
        public virtual bool OnReloading() => true;
        public virtual bool OnShooting() => true;
        public virtual bool OnZooming() => true;

        public virtual void OnReloaded() { }
        public virtual void OnShotNothing() { }
        public virtual void OnShotTarget(GameObject target) { }
        public virtual void OnZoomed() { }
        public virtual void OnInspected() { }

        private void RemoveShotAmmo()
        {
            if (m_AmmoPerShot > 0)
                m_CurAmmo -= m_AmmoPerShot;
        }
    }
}
