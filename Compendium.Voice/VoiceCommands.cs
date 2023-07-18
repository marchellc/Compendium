using BetterCommands;
using BetterCommands.Permissions;

using PluginAPI.Core;

using System.Linq;

namespace Compendium.Voice
{
    public static class VoiceCommands
    {
        private static PermissionData _overridePerms = new PermissionData(new string[] { "voice.override" }, PermissionNodeMode.AnyOf, PermissionLevel.Administrator);

        private static bool GrantOverride(Player sender)
        {
            var res = _overridePerms.Validate(sender.ReferenceHub);
            return res.IsSuccess;
        }

        [Command("playback", CommandType.RemoteAdmin, CommandType.PlayerConsole)]
        public static string Playback(Player sender)
        {
            if (VoiceController.Playback.Contains(sender.NetworkId))
            {
                VoiceController.m_Playback.Remove(sender.NetworkId);
                return "Playback disabled.";
            }
            else
            {
                VoiceController.m_Playback.Add(sender.NetworkId);
                return "Playback enabled.";
            }
        }

        [Command("ovmode", CommandType.PlayerConsole, CommandType.RemoteAdmin)]
        [CommandAliases("omode", "ovm")]
        public static string OvMode(Player sender)
        {
            if (VoiceController.m_OvFlags.TryGetValue(sender.NetworkId, out var flags))
            {
                if (flags is OverwatchVoiceFlags.AllScps)
                {
                    VoiceController.m_OvFlags[sender.NetworkId] = OverwatchVoiceFlags.TargetScp;
                    return "Switched to targeted SCPs.";
                }
                else
                {
                    VoiceController.m_OvFlags[sender.NetworkId] = OverwatchVoiceFlags.AllScps;
                    return "Switched to all SCPs.";
                }
            }
            else
            {
                VoiceController.m_OvFlags[sender.NetworkId] = OverwatchVoiceFlags.AllScps;
                return "Switched to all SCPs.";
            }
        }

        [Command("staffmode", CommandType.RemoteAdmin, CommandType.PlayerConsole)]
        [CommandAliases("smode")]
        public static string StaffMode(Player sender)
        {
            if (VoiceController.PriorityVoice is null)
                return "Staff Voice is not active.";

            if (VoiceController.PriorityVoice.netId != sender.NetworkId && !GrantOverride(sender))
                return "You are not in control of staff voice.";

            if (VoiceController.StaffFlags is StaffVoiceFlags.AllowNonStaffListen)
            {
                VoiceController.StaffFlags = StaffVoiceFlags.DisallowNonStaffListen;
                return "Regular players can't hear you now.";
            }
            else
            {
                VoiceController.StaffFlags = StaffVoiceFlags.AllowNonStaffListen;
                return "Regular players can hear you again.";
            }
        }

        [Command("globalvoice", CommandType.RemoteAdmin)]
        [CommandAliases("gvoice", "globalv", "gv")]
        [Permission(PermissionNodeMode.AnyOf, "voice.global")]
        public static string GlobalCommand(Player sender)
        {
            if (VoiceController.PriorityVoice is null)
            {
                VoiceController.PriorityVoice = sender.ReferenceHub;
                VoiceController.StaffFlags = StaffVoiceFlags.None;

                return "Priority Voice enabled.";
            }
            else
            {
                if (VoiceController.StaffFlags != StaffVoiceFlags.None)
                    return "Staff Voice is enabled! Disable it first.";


                if (VoiceController.PriorityVoice.netId != sender.NetworkId && !GrantOverride(sender))
                    return $"{VoiceController.PriorityVoice.nicknameSync.MyNick} is currently speaking.";
                else
                {
                    if (VoiceController.PriorityVoice.netId == sender.NetworkId)
                    {
                        VoiceController.PriorityVoice = null;
                        VoiceController.StaffFlags = StaffVoiceFlags.None;

                        return "Priority Voice disabled.";
                    }
                    else
                    {
                        VoiceController.PriorityVoice = sender.ReferenceHub;
                        VoiceController.StaffFlags = StaffVoiceFlags.None;

                        return "Priority Voice enabled for you.";
                    }
                }
            }
        }

        [Command("staffvoice", CommandType.RemoteAdmin)]
        [CommandAliases("svoice", "staffv", "sv")]
        [Permission(PermissionNodeMode.AnyOf, "voice.staff")]
        public static string StaffCommand(Player sender)
        {
            if (VoiceController.PriorityVoice is null)
            {
                VoiceController.PriorityVoice = sender.ReferenceHub;
                VoiceController.StaffFlags = StaffVoiceFlags.AllowNonStaffListen;

                return "You are now in control of Staff Voice. Regular players can hear you - use smode to change that.";
            }
            else
            {
                if (VoiceController.StaffFlags is StaffVoiceFlags.None)
                    return "Priority Voice is active! Disable it first.";

                if (VoiceController.PriorityVoice.netId != sender.NetworkId && !GrantOverride(sender))
                    return "You are not in control of Staff Voice!";

                if (VoiceController.PriorityVoice.netId == sender.NetworkId)
                {
                    VoiceController.PriorityVoice = null;
                    VoiceController.StaffFlags = StaffVoiceFlags.None;

                    return "Staff Voice disabled.";
                }
                else
                {
                    VoiceController.PriorityVoice = sender.ReferenceHub;
                    VoiceController.StaffFlags = StaffVoiceFlags.AllowNonStaffListen;

                    return "You are now in control of Staff Voice. Regular players can hear you - use smode to change that.";
                }
            }
        }
    }
}
