using Compendium.Helpers.Events;

using helpers;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;

namespace Compendium.Grab
{
    public static class GrabHandler
    {
        private static readonly Dictionary<ReferenceHub, IGrabTarget> m_Grabs = new Dictionary<ReferenceHub, IGrabTarget>();
        private static readonly object m_Lock = new object();

        public static object Lock => m_Lock;
        public static IReadOnlyDictionary<ReferenceHub, IGrabTarget> Targets => m_Grabs;

        public static void Load()
        {
            Reload();
            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", OnFixedUpdate);
            ServerEventType.RoundRestart.AddHandler<Action>(OnRoundRestart);
            GrabInput.Load();
        }

        public static void Unload()
        {
            Reload();
            Reflection.TryRemoveHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", OnFixedUpdate);
            ServerEventType.RoundRestart.RemoveHandler<Action>(OnRoundRestart);
            GrabInput.Unload();
        }

        public static void Reload()
        {
            lock (m_Lock)
            {
                m_Grabs.Clear();
            }
        }

        public static void Grab(ReferenceHub hub, IGrabTarget target)
        {
            Ungrab(hub);

            lock (m_Lock)
            {
                m_Grabs[hub] = target;
            }
        }

        public static void Ungrab(ReferenceHub hub)
        {
            var curTarget = GetTarget(hub);

            if (curTarget != null)
                curTarget.Release();

            lock (m_Lock)
            {
                m_Grabs[hub] = null;
            }
        }

        public static IGrabTarget GetTarget(ReferenceHub hub)
        {
            lock (m_Lock)
            {
                return m_Grabs.TryGetValue(hub, out var target) ? target : null;
            }
        }

        public static bool HasTarget(ReferenceHub hub) 
            => GetTarget(hub) != null;

        private static void OnRoundRestart()
        {
            lock (m_Lock)
            {
                m_Grabs.Clear();
            }
        }

        private static void OnFixedUpdate()
        {
            lock (m_Lock)
            {
                foreach (var pair in m_Grabs)
                {
                    if (pair.Value != null)
                    {
                        pair.Value.Move();
                    }
                }
            }
        }
    }
}