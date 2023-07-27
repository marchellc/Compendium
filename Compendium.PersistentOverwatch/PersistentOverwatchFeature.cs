using Compendium.Helpers;
using Compendium.Features;
using Compendium.Helpers.Calls;
using Compendium.Helpers.Colors;
using Compendium.Helpers.Events;

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
        public override string Name => "Persistent Overwatch";

        public static string StoragePath => $"{FeatureManager.DirectoryPath}/overwatch_storage";

        public static SingleFileStorage<string> Storage { get; set; }

        public override void Load()
        {
            base.Load();

            Storage = new SingleFileStorage<string>(StoragePath);
            Storage.Reload();

            Reflection.TryAddHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);

            ServerEventType.PlayerJoined.AddHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);

            FLog.Info($"Overwatch storage loaded.");
        }

        public override void Reload()
        {
            Storage?.Reload();
            FLog.Info($"Reloaded.");
        }

        public override void Unload()
        {
            base.Unload();

            Storage?.Save();
            Storage = null;

            Reflection.TryRemoveHandler<PlayerRoleManager.RoleChanged>(typeof(PlayerRoleManager), "OnRoleChanged", OnRoleChanged);

            ServerEventType.PlayerJoined.RemoveHandler<Action<PlayerJoinedEvent>>(OnPlayerJoined);

            FLog.Info($"Unloaded.");
        }

        private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
        {
            if (!FeatureManager.GetFeature<PersistentOverwatchFeature>().IsEnabled || Storage is null)
                return;

            if (prevRole.RoleTypeId is RoleTypeId.Overwatch)
            {
                if (newRole.RoleTypeId is RoleTypeId.Overwatch)
                    return;

                if (Storage.Delete(hub.characterClassManager.UserId))
                {
                    Storage.Save();
                    hub.Hint($"\n\n<b>Persistent Overwatch is now <color={ColorValues.Red}>disabled</color>.", 5f, true);
                }
            }
            else
            {
                if (newRole.RoleTypeId is RoleTypeId.Overwatch)
                {
                    if (Storage.Append(hub.characterClassManager.UserId))
                    {
                        Storage.Save();
                        hub.Hint($"\n\n<b>Persistent Overwatch is now <color={ColorValues.Green}>active</color>.</b>", 5f, true);
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
                    ev.Player.ReferenceHub.Hint(
                        $"\n\n<b><color={ColorValues.LightGreen}>[Persistent Overwatch]</color></b>\n" +
                        $"<b>Role changed to <color={ColorValues.Green}>Overwatch</color>.</b>", 3f, true);
                }, 0.7f);
            }
        }
    }
}