using Compendium.Features;
using Compendium.Constants;
using Compendium.Events;
using Compendium.IO.Saving;

using PlayerRoles;

using PluginAPI.Events;

namespace Compendium.PersistentOverwatch
{
    public class PersistentOverwatchFeature : FeatureBase
    {
        public override string Name => "Persistent Overwatch";

        public static SaveFile<CollectionSaveData<string>> Storage { get; set; }

        public override void Load()
        {
            base.Load();

            Storage = new SaveFile<CollectionSaveData<string>>(Directories.GetDataPath("SavedOverwatchPlayers", "overwatchPlayers"));

            PlayerRoleManager.OnRoleChanged += OnRoleChanged;

            FLog.Info($"Overwatch storage loaded.");
        }

        public override void Reload()
        {
            Storage?.Load();
            FLog.Info($"Reloaded.");
        }

        public override void Unload()
        {
            base.Unload();

            Storage?.Save();
            Storage = null;

            PlayerRoleManager.OnRoleChanged -= OnRoleChanged;

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

                if (Storage.Data.Remove(hub.UserId()))
                {
                    Storage.Save();
                    hub.Hint($"\n\n<b>Persistent Overwatch is now <color={Colors.RedValue}>disabled</color>.", 5f);
                }
            }
            else
            {
                if (newRole.RoleTypeId is RoleTypeId.Overwatch)
                {
                    if (!Storage.Data.Contains(hub.UserId()))
                    {
                        Storage.Data.Add(hub.UserId());
                        Storage.Save();

                        hub.Hint($"\n\n<b>Persistent Overwatch is now <color={Colors.GreenValue}>active</color>.</b>", 5f);
                    }
                }
            }
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (Storage is null)
                return;

            if (Storage.Data.Contains(ev.Player.UserId))
            {
                Calls.Delay(0.7f, () =>
                {
                    ev.Player.SetRole(RoleTypeId.Overwatch);
                    ev.Player.ReferenceHub.Hint(
                        $"\n\n<b><color={Colors.LightGreenValue}>[Persistent Overwatch]</color></b>\n" +
                        $"<b>Role changed to <color={Colors.GreenValue}>Overwatch</color>.</b>", 3f);
                });
            }
        }
    }
}