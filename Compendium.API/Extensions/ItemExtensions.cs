using helpers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Extensions
{
    public static class ItemExtensions
    {
        public static IReadOnlyList<ItemType> AllItems { get; } = Enum.GetValues(typeof(ItemType)).Cast<ItemType>().ToList().AsReadOnly();
        public static IReadOnlyList<ItemType> AllValidItems { get; } = AllItems.Where(i => i != ItemType.None).ToList().AsReadOnly();

        public static IReadOnlyList<ItemType> Ammo { get; } = AllValidItems.Where(i => i.IsAmmo()).ToList().AsReadOnly();
        public static IReadOnlyList<ItemType> Firearms { get; } = AllValidItems.Where(i => i.IsFirearm()).ToList().AsReadOnly();
        public static IReadOnlyList<ItemType> Explosives { get; } = AllValidItems.Where(i => i.IsExplosive()).ToList().AsReadOnly();
        public static IReadOnlyList<ItemType> Keycards { get; } = AllValidItems.Where(i => i.IsKeycard()).ToList().AsReadOnly();
        public static IReadOnlyList<ItemType> Medicals { get; } = AllValidItems.Where(i => i.IsMedical()).ToList().AsReadOnly();
        public static IReadOnlyList<ItemType> Usables { get; } = AllValidItems.Where(i => i.IsUsable()).ToList().AsReadOnly();
        public static IReadOnlyList<ItemType> Scps { get; } = AllValidItems.Where(i => i.IsScp()).ToList().AsReadOnly();

        public static bool IsAmmo(this ItemType item)
            => item is ItemType.Ammo12gauge || item is ItemType.Ammo44cal || item is ItemType.Ammo556x45 || item is ItemType.Ammo762x39 || item is ItemType.Ammo9x19;

        public static bool IsFirearm(this ItemType item, bool countMicroHid = true, bool countJailbird = true)
            => item is ItemType.GunA7 || item is ItemType.GunAK || item is ItemType.GunCOM15 || item is ItemType.GunCOM18 || item is ItemType.GunCom45
            || item is ItemType.GunCrossvec || item is ItemType.GunE11SR || item is ItemType.GunFRMG0 || item is ItemType.GunFSP9 || item is ItemType.GunLogicer
            || item is ItemType.GunRevolver || item is ItemType.GunShotgun || item is ItemType.ParticleDisruptor || (countMicroHid && item is ItemType.MicroHID) || (countJailbird && item is ItemType.Jailbird);

        public static bool IsExplosive(this ItemType item)
            => item is ItemType.GrenadeFlash || item is ItemType.GrenadeHE || item is ItemType.SCP018;

        public static bool IsArmor(this ItemType item)
            => item is ItemType.ArmorCombat || item is ItemType.ArmorHeavy || item is ItemType.ArmorLight;

        public static bool IsKeycard(this ItemType item)
            => item is ItemType.KeycardChaosInsurgency || item is ItemType.KeycardContainmentEngineer || item is ItemType.KeycardFacilityManager
            || item is ItemType.KeycardGuard || item is ItemType.KeycardJanitor || item is ItemType.KeycardMTFCaptain || item is ItemType.KeycardMTFOperative
            || item is ItemType.KeycardMTFPrivate || item is ItemType.KeycardO5 || item is ItemType.KeycardResearchCoordinator || item is ItemType.KeycardScientist
            || item is ItemType.KeycardZoneManager;

        public static bool IsMedical(this ItemType item)
            => item is ItemType.Medkit || item is ItemType.SCP500 || item is ItemType.Painkillers;

        public static bool IsUsable(this ItemType item)
            => item is ItemType.Adrenaline || item is ItemType.Medkit || item is ItemType.Painkillers 
            || item is ItemType.SCP500 || item is ItemType.SCP1576 || item is ItemType.SCP1853 || item is ItemType.SCP207 || item is ItemType.SCP2176
            || item is ItemType.SCP244a || item is ItemType.SCP244b || item is ItemType.SCP268 || item is ItemType.SCP330 || item is ItemType.SCP500
            || item is ItemType.AntiSCP207;

        public static bool IsScp(this ItemType item)
            => item is ItemType.SCP018 || item is ItemType.SCP1576 || item is ItemType.SCP1853 || item is ItemType.SCP207 || item is ItemType.SCP2176
            || item is ItemType.SCP244a || item is ItemType.SCP244b || item is ItemType.SCP268 || item is ItemType.SCP330 || item is ItemType.SCP500
            || item is ItemType.AntiSCP207;

        public static ItemCategory GetCategory(this ItemType item)
        {
            if (item.IsExplosive())
                return ItemCategory.Grenade;
            else if (item.IsAmmo())
                return ItemCategory.Ammo;
            else if (item.IsMedical())
                return ItemCategory.Medical;
            else if (item.IsArmor())
                return ItemCategory.Armor;
            else if (item.IsFirearm())
                return ItemCategory.Firearm;
            else if (item.IsKeycard())
                return ItemCategory.Keycard;
            else if (item.IsScp())
                return ItemCategory.SCPItem;
            else if (item == ItemType.Radio)
                return ItemCategory.Radio;
            else
                return ItemCategory.None;
        }

        public static ItemType GetAmmoType(this ItemType firearmType)
        {
            switch (firearmType)
            {
                case ItemType.GunA7:
                case ItemType.GunAK:
                case ItemType.GunLogicer:
                    return ItemType.Ammo762x39;

                case ItemType.GunCOM15:
                case ItemType.GunCOM18:
                case ItemType.GunCom45:
                case ItemType.GunFSP9:
                    return ItemType.Ammo9x19;

                case ItemType.GunE11SR:
                case ItemType.GunFRMG0:
                    return ItemType.Ammo556x45;

                case ItemType.GunRevolver:
                    return ItemType.Ammo44cal;

                case ItemType.GunShotgun:
                    return ItemType.Ammo12gauge;

                case ItemType.MicroHID:
                    return ItemType.MicroHID;

                case ItemType.ParticleDisruptor:
                    return ItemType.ParticleDisruptor;

                case ItemType.Jailbird:
                    return ItemType.Jailbird;

                default:
                    return ItemType.None;
            }
        }

        public static string GetName(this ItemType item)
        {
            switch (item)
            {
                case ItemType.Ammo12gauge:
                    return "12 gauge ammo";
                case ItemType.Ammo44cal:
                    return ".44 caliber ammo";
                case ItemType.Ammo556x45:
                    return "5.56 x 45mm ammo";
                case ItemType.Ammo762x39:
                    return "7.62 x 39mm ammo";
                case ItemType.Ammo9x19:
                    return "9 x 19mm ammo";

                case ItemType.AntiSCP207:
                    return "Anti-SCP-207";

                case ItemType.ArmorCombat:
                    return "Combat Armor";
                case ItemType.ArmorHeavy:
                    return "Heavy Armor";
                case ItemType.ArmorLight:
                    return "Light Armor";

                case ItemType.GrenadeFlash:
                    return "Flash Grenade";
                case ItemType.GrenadeHE:
                    return "Frag Grenade";

                case ItemType.GunA7:
                    return "A7";
                case ItemType.GunAK:
                    return "AK";
                case ItemType.GunCOM15:
                    return "COM-15";
                case ItemType.GunCOM18:
                    return "COM-18";
                case ItemType.GunCom45:
                    return "COM-45";
                case ItemType.GunCrossvec:
                    return "Crossvec";
                case ItemType.GunE11SR:
                    return "Epsilon E-11 SR";
                case ItemType.GunFRMG0:
                    return "FR-MG-0";
                case ItemType.GunFSP9:
                    return "FSP-9";
                case ItemType.GunLogicer:
                    return "Logicer";
                case ItemType.GunRevolver:
                    return ".44 Revolver";
                case ItemType.GunShotgun:
                    return "Shotgun";

                case ItemType.KeycardChaosInsurgency:
                    return "Chaos Insurgency Access Device";
                case ItemType.KeycardContainmentEngineer:
                    return "Containment Engineer Keycard";
                case ItemType.KeycardFacilityManager:
                    return "Facility Manager Keycard";
                case ItemType.KeycardGuard:
                    return "Facility Guard Keycard";
                case ItemType.KeycardJanitor:
                    return "Janitor Keycard";
                case ItemType.KeycardMTFCaptain:
                    return "MTF Captain Keycard";
                case ItemType.KeycardMTFOperative:
                    return "MTF Operative Keycard";
                case ItemType.KeycardMTFPrivate:
                    return "MTF Private Keycard";
                case ItemType.KeycardO5:
                    return "O-5 Keycard";
                case ItemType.KeycardResearchCoordinator:
                    return "Research Coordinator Keycard";
                case ItemType.KeycardScientist:
                    return "Scientist Keycard";
                case ItemType.KeycardZoneManager:
                    return "Zone Manager Keycard";

                case ItemType.MicroHID:
                    return "Micro-H.I.D.";

                case ItemType.ParticleDisruptor:
                    return "3-X Particle Disruptor";

                case ItemType.SCP018:
                    return "SCP-018";
                case ItemType.SCP1576:
                    return "SCP-1576";
                case ItemType.SCP1853:
                    return "SCP-1853";
                case ItemType.SCP207:
                    return "SCP-207";
                case ItemType.SCP2176:
                    return "SCP-2176";
                case ItemType.SCP244a:
                    return "SCP-244-A";
                case ItemType.SCP244b:
                    return "SCP-244-B";
                case ItemType.SCP268:
                    return "SCP-268";
                case ItemType.SCP330:
                    return "SCP-330";
                case ItemType.SCP500:
                    return "SCP-500";

                default:
                    return item.ToString();
            }
        }
    }
}
