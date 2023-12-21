using BetterCommands;
using BetterCommands.Permissions;
using Compendium.Processors;

using PlayerRoles.FirstPersonControl;

namespace Compendium.Custom.Commands
{
    public static class FunCommands
    {
        [Command("bones", CommandType.GameConsole, CommandType.RemoteAdmin)]
        [Description("Spawns a skeleton at the specified player.")]
        [Permission(PermissionLevel.Lowest)]
        public static string BonesCommand(ReferenceHub sender, ReferenceHub target)
        {
            if (target.roleManager.CurrentRole is not IFpcRole fpcRole)
                return "The targeted player is not playing as a first-person role.";

            HalloweenUtils.SpawnBones(fpcRole.FpcModule.Position);
            return $"Spawned a skeleton at {target.Nick()}";
        }

        [Command("rocket", CommandType.RemoteAdmin)]
        [Description("Sends a player into space.")]
        [Permission(PermissionLevel.Lowest)]
        public static string RocketCommand(ReferenceHub sender, ReferenceHub target)
        {
            if (RocketProcessor.IsActive(target))
            {
                RocketProcessor.Remove(target);
                return $"Rocket of {target.Nick()} disabled.";
            }
            else
            {
                RocketProcessor.Add(target);
                return $"Sent {target.Nick()} into space.";
            }
        }
    }
}