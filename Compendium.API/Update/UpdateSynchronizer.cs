using Compendium.Events;
using Compendium.Extensions;

using System;

using UnityEngine;

namespace Compendium.Update
{
    public class UpdateSynchronizer : MonoBehaviour
    {
        public static int LastFrameDuration { get; private set; }
        public static UpdateSynchronizer Synchronizer { get; private set; }

        internal static event Action OnUpdate;

        void Update()
        {
            LastFrameDuration = (Mathf.RoundToInt(Time.deltaTime) / 100f) < 1f ? 1 : Mathf.RoundToInt(Time.deltaTime);
            OnUpdate?.Invoke();
        }

        [Event(PluginAPI.Enums.ServerEventType.WaitingForPlayers)]
        private static void Load()
        {
            if (ReferenceHub.HostHub is null)
            {
                Plugin.Warn($"Failed to enable update synchronizer: HostHub is still null.");
                return;
            }

            if (Synchronizer != null)
                ReferenceHub.HostHub.DestroyComponent<UpdateSynchronizer>();

            Synchronizer = ReferenceHub.HostHub.GetOrAddComponent<UpdateSynchronizer>();
        }
    }
}