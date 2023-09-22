using Compendium.Extensions;
using Compendium.Events;
using Compendium.Round;

using helpers;

using Mirror;

using PluginAPI.Enums;

using System.Collections.Generic;

using UnityEngine;

namespace Compendium.Prefabs
{
    public static class PrefabHelper
    {
        private static readonly Dictionary<PrefabName, string> m_Names = new Dictionary<PrefabName, string>()
        {
            [PrefabName.Player] = "Player",

            [PrefabName.AntiScp207] = "AntiSCP207Pickup",
            [PrefabName.Adrenaline] = "AdrenalinePrefab",
            [PrefabName.Ak] = "AkPickup",
            [PrefabName.A7] = "A7Pickup",
            [PrefabName.Ammo12ga] = "Ammo12gaPickup",
            [PrefabName.Ammo44cal] = "Ammo44calPickup",
            [PrefabName.Ammo556mm] = "Ammo556mmPickup",
            [PrefabName.Ammo762mm] = "Ammo762mmPickup",
            [PrefabName.Ammo9mm] = "Ammo9mmPickup",
            [PrefabName.ChaosKeycard] = "ChaosKeycardPickup",
            [PrefabName.Coin] = "CoinPickup",
            [PrefabName.Com15] = "Com15Pickup",
            [PrefabName.Com18] = "Com18Pickup",
            [PrefabName.Com45] = "Com45Pickup",
            [PrefabName.CombatArmor] = "Combat Armor Pickup",
            [PrefabName.Crossvec] = "CrossvecPickup",
            [PrefabName.Disruptor] = "DisruptorPickup",
            [PrefabName.Epsilon11SR] = "E11SRPickup",
            [PrefabName.FlashbangPickup] = "FlashbangPickup",
            [PrefabName.FlashbangProjectile] = "FlashbangProjectile",
            [PrefabName.Flashlight] = "FlashlightPickup",
            [PrefabName.Fsp9] = "Fsp9Pickup",
            [PrefabName.FrMg0] = "FRMG0Pickup",
            [PrefabName.HeavyArmor] = "Heavy Armor Pickup",
            [PrefabName.HegPickup] = "HegPickup",
            [PrefabName.HegProjectile] = "HegProjectile",
            [PrefabName.Jailbird] = "JailbirdPickup",
            [PrefabName.LightArmor] = "Light Armor Pickup",
            [PrefabName.Logicer] = "LogicerPickup",
            [PrefabName.Medkit] = "MedkitPickup",
            [PrefabName.MicroHid] = "MicroHidPickup",
            [PrefabName.Painkillers] = "PainkillersPickup",
            [PrefabName.Radio] = "RadioPickup",
            [PrefabName.RegularKeycard] = "RegularKeycardPickup",
            [PrefabName.Revolver] = "RevolverPickup",
            [PrefabName.Scp1576] = "SCP1576Pickup",
            [PrefabName.Scp1853] = "SCP1853Pickup",
            [PrefabName.Scp207] = "SCP207Pickup",
            [PrefabName.Scp244a] = "SCP244APickup Variant",
            [PrefabName.Scp244b] = "SCP244BPickup Variant",
            [PrefabName.Scp268] = "SCP268Pickup",
            [PrefabName.Scp500] = "SCP500Pickup",
            [PrefabName.Scp018] = "Scp018Projectile",
            [PrefabName.Scp2176] = "Scp2176Projectile",
            [PrefabName.Scp330] = "Scp330Pickup",
            [PrefabName.Shotgun] = "ShotgunPickup",

            [PrefabName.HealthBox] = "AdrenalineMedkitStructure",
            [PrefabName.Generator] = "GeneratorStructure",
            [PrefabName.LargeGunLocker] = "LargeGunLockerStructure",
            [PrefabName.MiscLocker] = "MiscLocker",
            [PrefabName.MedkitBox] = "RegularMedkitStructure",
            [PrefabName.RifleRack] = "RifleRackStructure",

            [PrefabName.Scp018Pedestal] = "Scp018PedestalStructure Variant",
            [PrefabName.Scp1853Pedestal] = "Scp1853PedestalStructure Variant",
            [PrefabName.Scp207Pedestal] = "Scp207PedestalStructure Variant",
            [PrefabName.Scp2176Pedestal] = "Scp2176PedestalStructure Variant",
            [PrefabName.Scp244Pedestal] = "Scp244PedestalStructure Variant",
            [PrefabName.Scp268Pedestal] = "Scp268PedestalStructure Variant",
            [PrefabName.Scp500Pedestal] = "Scp500PedestalStructure Variant",
            [PrefabName.Scp1576Pedestal] = "Scp1576PedestalStructure Variant",

            [PrefabName.AmnesticCloud] = "Amnestic Cloud Hazard",
            [PrefabName.WorkStation] = "Spawnable Work Station Structure",

            [PrefabName.Tantrum] = "TantrumObj",

            [PrefabName.EntranceZoneDoor] = "EZ BreakableDoor",
            [PrefabName.HeavyContainmentZoneDoor] = "HCZ BreakableDoor",
            [PrefabName.LightContainmentZoneDoor] = "LCZ BreakableDoor",

            [PrefabName.SportTarget] = "sportTargetPrefab",
            [PrefabName.ClassDTarget] = "dboyTargetPrefab",
            [PrefabName.BinaryTarget] = "binaryTargetPrefab",

            [PrefabName.PrimitiveObject] = "PrimitiveObjectToy",
            [PrefabName.LightSource] = "LightSourceToy",

            [PrefabName.Ragdoll1] = "Ragdoll_1",
            [PrefabName.Ragdoll4] = "Ragdoll_4",
            [PrefabName.Ragdoll6] = "Ragdoll_6",
            [PrefabName.Ragdoll7] = "Ragdoll_7",
            [PrefabName.Ragdoll8] = "Ragdoll_8",
            [PrefabName.Ragdoll10] = "Ragdoll_10",
            [PrefabName.Ragdoll12] = "Ragdoll_12",
            [PrefabName.Scp096Ragdoll] = "SCP-096_Ragdoll",
            [PrefabName.Scp106Ragdoll] = "SCP-106_Ragdoll",
            [PrefabName.Scp173Ragdoll] = "SCP-173_Ragdoll",
            [PrefabName.Scp939Ragdoll] = "SCP-939_Ragdoll",
            [PrefabName.TutorialRagdoll] = "Ragdoll_Tut",
        };

        private static readonly Dictionary<PrefabName, GameObject> m_Prefabs = new Dictionary<PrefabName, GameObject>();

        public static bool TryInstantiatePrefab<TComponent>(PrefabName name, out TComponent component) where TComponent : Component
        {
            if (!TryInstantiatePrefab(name, out var instance))
            {
                component = null;
                return false;
            }

            return instance.TryGet(out component);
        }

        public static bool TryInstantiatePrefab(PrefabName name, out GameObject instance)
        {
            if (!TryGetPrefab(name, out var prefab))
            {
                instance = null;
                return false;
            }

            instance = Object.Instantiate(prefab);
            return instance != null;
        }

        public static bool TryGetPrefab(PrefabName name, out GameObject prefab)
        {
            if (m_Prefabs.Count == 0)
                LoadPrefabs();

            return m_Prefabs.TryGetValue(name, out prefab);
        }

        [RoundStateChanged(RoundState.Restarting)]
        public static void ClearPrefabs() 
            => m_Prefabs.Clear();

        [Event(ServerEventType.MapGenerated)]
        public static void ReloadPrefabs()
        {
            ClearPrefabs();
            LoadPrefabs();
        }

        private static void LoadPrefabs()
        {
            m_Prefabs[PrefabName.Player] = NetworkManager.singleton.playerPrefab;

            foreach (var prefab in NetworkClient.prefabs.Values)
            {
                if (m_Names.TryGetKey(prefab.name, out var prefabType))
                {
                    if (prefabType is PrefabName.Player)
                        continue;

                    m_Prefabs[prefabType] = prefab;
                }
                else
                {
                    Plugin.Warn($"Failed to retrieve prefab name: {prefab.name}");
                }
            }

            Plugin.Info($"Loaded {m_Prefabs.Count} / {m_Names.Count} prefabs.");
        }
    }
}