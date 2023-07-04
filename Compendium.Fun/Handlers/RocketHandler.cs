using System;
using System.Collections.Generic;

namespace Compendium.Fun.Handlers
{
    public static class RocketHandler
    {
        private static readonly Dictionary<ReferenceHub, RocketProperties> m_Rockets = new Dictionary<ReferenceHub, RocketProperties>();
        private static readonly object m_Lock = new object();

        public static void Reload()
        {
            Lock(dict => dict.Clear());
        }

        public static void Load()
        {
            Lock(dict => dict.Clear());
        }

        public static void Unload()
        {
            Lock(dict => dict.Clear());
        }

        public static bool IsActive(ReferenceHub hub)
            => m_Rockets.ContainsKey(hub);

        public static void SetActive(ReferenceHub hub, bool state)
        {
            if (!state)
                Lock(dict => dict.Remove(hub));
            else
                Lock(dict =>
                {
                    dict[hub] = new RocketProperties()
                    {
                        BasePosition = hub.PlayerCameraReference.position,
                        BaseRotation = hub.PlayerCameraReference.rotation.eulerAngles,
                        MaxHeight = hub.PlayerCameraReference.position.y + 500f
                    };
                });
        }

        private static void OnFixedUpdate()
        {
            Lock(dict =>
            {
                foreach (var player in dict)
                {

                }
            });
        }

        private static void Lock(Action<Dictionary<ReferenceHub, RocketProperties>> action)
        {
            lock (m_Lock)
            {
                action?.Invoke(m_Rockets);
            }
        }
    }
}