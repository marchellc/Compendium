using BetterCommands;

using helpers.Configuration.Ini;
using helpers.Patching;

using PluginAPI.Core;

using Respawning;

namespace Compendium.Gameplay.Respawning
{
    public static class RespawnController
    {
        [IniConfig(Name = "Respawn Enabled", Description = "Whether or not to enable team respawning.")]
        public static bool IsEnabled { get; set; } = true;

        [Patch(typeof(RespawnManager), nameof(RespawnManager.Update), PatchType.Prefix, "Respawn Patch")]
        private static bool _RespawnPatch() => IsEnabled;

        [Command("switchrespawns", CommandType.RemoteAdmin, CommandType.GameConsole)]
        private static string SwitchRespawns(Player sender)
        {
            IsEnabled = !IsEnabled;

            if (IsEnabled)
                return "Enabled team respawns.";
            else
                return "Disabled team respawns.";
        }
    }
}