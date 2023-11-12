using Compendium.Attributes;
using Compendium.Events;
using Compendium.Updating;

using PluginAPI.Events;

using System.Collections.Generic;

namespace Compendium.Processors
{
    public static class RocketProcessor
    {
        private static List<ReferenceHub> Active = new List<ReferenceHub>();

        private static object Lock = new object();

        public static bool IsActive(ReferenceHub hub)
        {
            lock (Lock) 
                return Active.Contains(hub);
        }

        public static void Add(ReferenceHub hub)
        {
            lock (Lock)
            {
                if (!Active.Contains(hub))
                    Active.Add(hub);
            }
        }

        public static void Remove(ReferenceHub hub)
        {
            lock (Lock)
                Active.Remove(hub);
        }

        [Event]
        private static void OnDeath(PlayerDeathEvent ev)
        {
            lock (Lock)
                Active.Remove(ev.Player.ReferenceHub);
        }

        [Event]
        private static void OnLeft(PlayerLeftEvent ev)
        {
            lock (Lock)
                Active.Remove(ev.Player.ReferenceHub);
        }

        [RoundStateChanged(Enums.RoundState.Restarting)]
        private static void OnRestart()
        {
            lock (Lock)
                Active.Clear();
        }

        [Update]
        private static void Update()
        {
            lock (Lock)
            {
                for (int i = 0; i < Active.Count; i++)
                {
                    var pos = Active[i].Position();
                    pos.y += 1.2f;
                    Active[i].Position(pos);
                }
            }
        }
    }
}