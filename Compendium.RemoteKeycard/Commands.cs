using BetterCommands;

using Compendium.RemoteKeycard.Handlers;

namespace Compendium.RemoteKeycard
{
    public static class Commands
    {
        public enum ToggleName
        {
            Warhead,
            Door,
            Generator,
            Locker,
            Shot,
            Remote
        }

        [Command("rktoggle", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Toggles a specific function of the Remote Keycard plugin.")]
        public static string RkToggle(ReferenceHub sender, ToggleName toggleName)
        {
            switch (toggleName)
            {
                case ToggleName.Warhead:
                    RoundSwitches.IsWarheadDisabled = !RoundSwitches.IsWarheadDisabled;
                    return RoundSwitches.IsWarheadDisabled ? $"Warhead interactions disabled." : "Warhead interactions enabled.";

                case ToggleName.Door:
                    RoundSwitches.IsDoorDisabled = !RoundSwitches.IsDoorDisabled;
                    return RoundSwitches.IsDoorDisabled ? $"Door interactions disabled." : "Door interactions enabled.";

                case ToggleName.Generator:
                    RoundSwitches.IsGeneratorDisabled = !RoundSwitches.IsGeneratorDisabled;
                    return RoundSwitches.IsGeneratorDisabled ? $"Generator interactions disabled." : "Generator interactions enabled.";

                case ToggleName.Locker:
                    RoundSwitches.IsLockerDisabled = !RoundSwitches.IsLockerDisabled;
                    return RoundSwitches.IsLockerDisabled ? $"Locker interactions disabled." : "Locker interactions enabled.";

                case ToggleName.Shot:
                    RoundSwitches.IsShotDisabled = !RoundSwitches.IsShotDisabled;
                    return RoundSwitches.IsShotDisabled ? $"Shot interactions disabled." : "Shot interactions enabled.";

                case ToggleName.Remote:
                    RoundSwitches.IsRemote = !RoundSwitches.IsRemote;
                    return RoundSwitches.IsRemote ? $"Remote interactions disabled." : "Warhead interactions enabled.";

                default:
                    return "Invalid toggle name!";
            }
        }
    }
}
