using Compendium.Attributes;
using Compendium.Helpers.Events;
using Compendium.Helpers.Prefabs;

using helpers.Extensions;
using helpers.Random;

using Mirror;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;

namespace Compendium.Npc
{
    public static class NpcManager
    {
        private static readonly HashSet<INpc> m_Spawned = new HashSet<INpc>();
        private static readonly HashSet<INpc> m_Despawned = new HashSet<INpc>();
        private static readonly HashSet<INpc> m_All = new HashSet<INpc>();

        public static IReadOnlyCollection<INpc> Spawned => m_Spawned;
        public static IReadOnlyCollection<INpc> Despawned => m_Despawned;
        public static IReadOnlyCollection<INpc> All => m_All;

        public static ReferenceHub NewHub
        {
            get
            {
                if (!PrefabHelper.TryInstantiatePrefab<ReferenceHub>(PrefabName.Player, out var hub))
                    return null;

                NetworkServer.AddPlayerForConnection(new NpcConnection(RandomGeneration.Default.GetRandom(0, 9999)), hub.gameObject);

                return hub;
            }
        }

        [InitOnLoad]
        internal static void Initialize()
        {
            ServerEventType.RoundEnd.AddHandler<Action>(OnRoundEnd);
        }

        internal static void OnNpcCreated(INpc npc)
        {
            m_All.Add(npc);
        }

        internal static void OnNpcDespawned(INpc npc)
        {
            m_Spawned.Remove(npc);
            m_Despawned.Add(npc);
        }

        internal static void OnNpcDestroyed(INpc npc)
        {
            m_Despawned.Remove(npc);
            m_Spawned.Remove(npc);
            m_All.Remove(npc);
        }

        internal static void OnNpcSpawned(INpc npc)
        {
            m_Despawned.Remove(npc);
            m_Spawned.Add(npc);
        }

        internal static void OnRoundEnd()
        {
            m_All.ForEach(npc =>
            {
                npc.Destroy();
            });

            m_All.Clear();
            m_Spawned.Clear();
            m_Despawned.Clear();
        }
    }
}