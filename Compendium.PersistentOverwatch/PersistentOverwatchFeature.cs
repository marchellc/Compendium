using Compendium.Features;
using Compendium.Helpers.Calls;
using Compendium.Helpers.Events;
using Compendium.Helpers.Overlay;

using helpers;
using helpers.IO.Storage;

using PlayerRoles;

using PluginAPI.Enums;
using PluginAPI.Events;

using System;

namespace Compendium.PersistentOverwatch
{
    public class PersistentOverwatchFeature : IFeature
    {
        public string Name => "Persistent Overwatch";

        public static bool IsEnabled { get; set; }
        public static string StoragePath => $"{FeatureManager.DirectoryPath}/overwatch_storage";

        public static SingleFileStorage<string> Storage { get; set; }

        public void Load()
        {
            Storage = new SingleFileStorage<string>(StoragePath);
            Storage.Reload();

            IsEnabled = true;

            Reflection.TryAddHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);

            ServerEventType.PlayerJoined.AddHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);

            this.Info($"Overwatch storage loaded.");
        }

        public void Reload()
        {
            Storage?.Reload();
            this.Info($"Reloaded.");
        }

        public void Unload()
        {
            Storage?.Save();
            Storage = null;

            IsEnabled = false;

            Reflection.TryRemoveHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);

            ServerEventType.PlayerJoined.RemoveHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);

            this.Info($"Unloaded.");
        }

        private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
        {
            if (!IsEnabled || Storage is null)
                return;

            if (prevRole.RoleTypeId is RoleTypeId.Overwatch)
            {
                if (newRole.RoleTypeId is RoleTypeId.Overwatch)
                    return;

                if (Storage.Remove(hub.characterClassManager.UserId))
                {
                    Storage.Save();
                    hub.ShowMessage($"Persistent Overwatch is no longer active.", 5f, true);
                }
            }
            else
            {
                if (newRole.RoleTypeId is RoleTypeId.Overwatch)
                {
                    if (Storage.Add(hub.characterClassManager.UserId))
                    {
                        Storage.Save();
                        hub.ShowMessage($"Persistent Overwatch is now active.", 5f, true);
                    }
                }
            }
        }

        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (!IsEnabled || Storage is null)
                return;

            if (Storage.Contains(ev.Player.UserId))
            {
                CallHelper.CallWithDelay(() =>
                {
                    ev.Player.SetRole(RoleTypeId.Overwatch);
                }, 0.2f);
            }
        }
    }
}