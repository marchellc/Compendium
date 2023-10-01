using BetterCommands;
using BetterCommands.Permissions;

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

        public static event Action OnUpdate;

        void Update()
        {
            LastFrameDuration = Mathf.CeilToInt(Time.deltaTime * 1000f);

            try
            {
                OnUpdate?.Invoke();
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to execute OnUpdate event!");
                Plugin.Error(ex);
            }
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
            Plugin.Info($"Update Synchronizer loaded.");
        }

        [Command("tps", CommandType.RemoteAdmin, CommandType.GameConsole, CommandType.PlayerConsole)]
        [Description("Shows the current value of server's ticks per second.")]
        private static string GetTpsCommand(ReferenceHub sender)
            => 
            $"TPS: {World.Ticks} / {World.TicksPerSecondFull} TPS\n" +
            $"Frame time: {World.Frametime} / {World.FrametimeFull} ms\n" +
            $"Synchronizer: {LastFrameDuration} ms";

        [Command("settps", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Administrator)]
        [Description("Sets the server's maximum ticks per second.")]
        private static string SetTpsCommand(ReferenceHub sender, int tps)
        {
            Application.targetFrameRate = tps;
            return $"TPS set to {Application.targetFrameRate}";
        }
    }
}