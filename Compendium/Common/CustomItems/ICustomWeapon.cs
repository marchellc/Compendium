using UnityEngine;

namespace Compendium.Common.CustomItems
{
    public interface ICustomWeapon : ICustomItem
    {
        ItemType AmmoType { get; }

        int MaxAmmo { get; set; }
        int CurAmmo { get; set; }
        int AmmoPerShot { get; set; }

        bool OnShooting();
        void OnShotTarget(GameObject target);
        void OnShotNothing();

        bool OnReloading();
        void OnReloaded();

        bool OnZooming();
        void OnZoomed();

        bool OnInspecting();
        void OnInspected();
    }
}