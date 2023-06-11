using Compendium.Extensions;
using helpers.Extensions;

using Mirror;

using System.Collections.Generic;

using UnityEngine;

namespace Compendium.Helpers.Prefabs
{
    public static class PrefabHelper
    {
        private static readonly Dictionary<PrefabName, string> m_Names = new Dictionary<PrefabName, string>()
        {

        };

        private static readonly Dictionary<PrefabName, GameObject> m_Prefabs = new Dictionary<PrefabName, GameObject>();

        public static bool TryInstantiatePrefab<TComponent>(PrefabName name, out TComponent component) where TComponent : Component
        {
            if (!TryInstantiatePrefab(name, out var instance))
            {
                component = null;
                return false;
            }

            return instance.TryGetComponentAllLayers(out component);
        }

        public static bool TryInstantiatePrefab(PrefabName name, out GameObject instance)
        {
            if (!TryGetPrefab(name, out var prefab))
            {
                instance = null;
                return false;
            }

            instance = GameObject.Instantiate(prefab);
            return instance != null;
        }

        public static bool TryGetPrefab(PrefabName name, out GameObject prefab)
        {
            if (m_Prefabs.Count == 0)
                LoadPrefabs();

            return m_Prefabs.TryGetValue(name, out prefab);
        }

        public static void ClearPrefabs() => m_Prefabs.Clear();
        public static void ReloadPrefabs()
        {
            ClearPrefabs();
            LoadPrefabs();
        }

        private static void LoadPrefabs()
        {
            foreach (var prefab in NetworkClient.prefabs.Values)
            {
                if (m_Names.TryGetKey(prefab.name, out var prefabType))
                {
                    m_Prefabs[prefabType] = prefab;
                }
            }
        }
    }
}