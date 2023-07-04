using BetterCommands;
using BetterCommands.Permissions;

using PluginAPI.Core;

namespace Compendium.Voice
{
    public static class VoiceCommands
    {
        [Command("globalvoice", CommandType.RemoteAdmin)]
        [CommandAliases("gvoice", "globalv", "gv")]
        [Permission(PermissionNodeMode.AnyOf, "voice.global")]
        public static string GlobalCommand(Player sender)
        {
            if (VoiceController.GlobalSpeaker != null && VoiceController.GlobalVoiceFlags is GlobalVoiceFlags.SpeakerOnly)
            {
                if (VoiceController.GlobalSpeaker.netId != sender.NetworkId)
                {
                    return $"Someone else is currently speaking.";
                }
                else
                {
                    VoiceController.GlobalSpeaker = null;
                    VoiceController.GlobalVoiceFlags = GlobalVoiceFlags.None;

                    return $"Stopped global speech.";
                }
            }
            else
            {
                VoiceController.GlobalSpeaker = sender.ReferenceHub;
                VoiceController.GlobalVoiceFlags = GlobalVoiceFlags.SpeakerOnly;

                return $"Started global speech.";
            }
        }

        [Command("staffvoice", CommandType.RemoteAdmin)]
        [CommandAliases("svoice", "staffv", "sv")]
        [Permission(PermissionNodeMode.AnyOf, "voice.staff")]
        public static string StaffCommand(Player sender)
        {
            if (VoiceController.GlobalVoiceFlags is GlobalVoiceFlags.StaffOnly)
            {
                VoiceController.GlobalVoiceFlags = GlobalVoiceFlags.None;
                return $"Disabled staff-only voice chat.";
            }
            else
            {
                VoiceController.GlobalVoiceFlags = GlobalVoiceFlags.StaffOnly;
                return $"Enabled staff-only voice chat.";
            }
        }
    }
}
