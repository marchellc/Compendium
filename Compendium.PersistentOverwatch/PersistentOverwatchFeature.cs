using Compendium.Features;
using Compendium.Helpers.Calls;
using Compendium.Helpers.Colors;
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
    public class PersistentOverwatchFeature : FeatureBase
    {
        public string Name => "Persistent Overwatch";

        public static string StoragePath => $"{FeatureManager.DirectoryPath}/overwatch_storage";

        public static SingleFileStorage<string> Storage { get; set; }

        public override void Load()
        {
            base.Load();

            Storage = new SingleFileStorage<string>(StoragePath);
            Storage.Reload();

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
            base.Unload();

            Storage?.Save();
            Storage = null;

            Reflection.TryRemoveHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);

            ServerEventType.PlayerJoined.RemoveHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);

            this.Info($"Unloaded.");
        }

        private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
        {
            if (!FeatureManager.GetFeature<PersistentOverwatchFeature>().IsEnabled || Storage is null)
                return;

            if (prevRole.RoleTypeId is RoleTypeId.Overwatch)
            {
                if (newRole.RoleTypeId is RoleTypeId.Overwatch)
                    return;

                if (Storage.Remove(hub.characterClassManager.UserId))
                {
                    Storage.Save();
                    hub.ShowMessage($"\n\n<b>Persistent Overwatch is now <color={ColorValues.Red}>disabled</color>.", 5f, 110);
                }
            }
            else
            {
                if (newRole.RoleTypeId is RoleTypeId.Overwatch)
                {
                    if (Storage.Add(hub.characterClassManager.UserId))
                    {
                        Storage.Save();
                        hub.ShowMessage($"\n\n<b>Persistent Overwatch is now <color={ColorValues.Green}>active</color>.</b>", 5f, 110);
                    }
                }
            }
        }

        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (!FeatureManager.GetFeature<PersistentOverwatchFeature>().IsEnabled || Storage is null)
                return;

            if (Storage.Contains(ev.Player.UserId))
            {
                CallHelper.CallWithDelay(() =>
                {
                    ev.Player.SetRole(RoleTypeId.Overwatch);
                    ev.Player.ReferenceHub.ShowMessage(
                        $"\n\n<b><color={ColorValues.LightGreen}>[Persistent Overwatch]</color></b>\n" +
                        $"<b>Role changed to <color={ColorValues.Green}>Overwatch</color>.</b>", 3f, 180);
                }, 0.7f);
            }
        }
    }
}