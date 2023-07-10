using Compendium.Helpers.Colors;
using Compendium.Helpers.Overlay;
using Compendium.Input;
using Compendium.Voice.Profiles;

using helpers;
using helpers.Extensions;

using PlayerRoles;
using PlayerRoles.Spectating;

using System;

namespace Compendium.Voice
{
    public static class VoiceUtils
    {
        public static void Load()
        {
            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", OnUpdate);
        }

        private static void OnUpdate()
        {
            if (!VoiceConfigs.UpdateHints)
                return;

            if (VoiceController._isRestarting)
                return;

            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (VoiceConfigs.AllowOverwatchScpChat
                    && hub.GetRoleId() is RoleTypeId.Overwatch
                    && TryGetSpectateTarget(hub, out var target)
                    && ValidateOvTarget(hub, target))
                {
                    UpdateOverwatchHint(hub, target);
                    continue;
                }

                if (VoiceController.TryGetProfile(hub, out var profile) 
                    && profile is ScpVoiceProfile voiceProfile)
                {
                    if (voiceProfile.IsProximityActive)
                    {
                        hub.ShowMessage(
                            $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Proximity voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                            $"Use <color={ColorValues.Red}>{GetProximitySwitchKeyString(hub)}</color> to switch back to <color={ColorValues.Red}>SCP chat</color>.</size>", 3f, 100);
                        continue;
                    }
                    else if (voiceProfile.IsProximityAvailable())
                    {
                        hub.ShowMessage(
                            $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>SCP chat</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                            $"Use <color={ColorValues.Red}>{GetProximitySwitchKeyString(hub)}</color> to switch to <color={ColorValues.Red}>Proximity voice</color>.</size>", 3f, 100);
                        continue;
                    }

                    continue;
                }

                if (VoiceController.PriorityVoice != null)
                {
                    if (VoiceController.StaffFlags is StaffVoiceFlags.None)
                    {
                        if (hub.netId == VoiceController.PriorityVoice.netId)
                        {
                            hub.ShowMessage(
                                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Global Voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                                $"<b>Only <color={ColorValues.Red}>you</color> can speak.</b></size>", 2f, 255);
                            continue;
                        }
                        else
                        {
                            hub.ShowMessage(
                                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Global Voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                                $"<b>Only <color={ColorValues.Red}>{VoiceController.PriorityVoice.nicknameSync.Network_myNickSync}</color> can speak.</b></color>", 3f, 255);
                            continue;
                        }
                    }
                    else
                    {
                        if (hub.netId == VoiceController.PriorityVoice.netId)
                        {
                            hub.ShowMessage(
                                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Staff Voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                                $"<b>Only <color={ColorValues.Red}>you</color> can control it.</b>\n" +
                                $"<b>Use the <color={ColorValues.Green}>staffmode</color> command to allow/disallow other players to listen.</b></size>", 3f, 255);
                            continue;
                        }
                        else
                        {
                            hub.ShowMessage(
                                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Staff Voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                                $"<b>Only <color={ColorValues.Red}>{VoiceController.PriorityVoice.nicknameSync.Network_myNickSync}</color> can control it.</b></size>", 3f, 255);
                            continue;
                        }
                    }
                }

                if (VoiceController.CanHearSelf(hub))
                {
                    hub.ShowMessage(
                        $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Microphone Playback</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                        $"<b>Use the <color={ColorValues.Red}>.playback</color> command to disable it.</b></sze>", 3f, 90);
                    continue;
                }
            }
        }

        private static void UpdateOverwatchHint(ReferenceHub hub, ReferenceHub target)
            => hub.ShowMessage(
                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Overwatch</color> allows you to hear other SCPs talk.</b>\n" +
                $"Use the <b><color={ColorValues.Red}>ovmode</color></b> command to switch between listening to all SCPs and targeted SCPs.\n" +
                $"<i>Current target: {GetOvTargetString(hub, target)}</i></size>", 3f, 100);

        private static bool ValidateOvTarget(ReferenceHub hub, ReferenceHub target)
        {
            if (!VoiceController.OverwatchFlags.TryGetValue(hub.netId, out var flags))
                flags = OverwatchVoiceFlags.TargetScp;

            if (flags is OverwatchVoiceFlags.AllScps)
                return target.IsSCP();
            else
                return target.IsSpectatedBy(hub);
        }

        private static string GetOvTargetString(ReferenceHub hub, ReferenceHub target)
        {
            if (!VoiceController.OverwatchFlags.TryGetValue(hub.netId, out var flags))
                flags = OverwatchVoiceFlags.TargetScp;

            if (flags is OverwatchVoiceFlags.AllScps)
                return $"{target.LoggedNameFromRefHub()} - ALL SCPS";
            else
                return $"{target.LoggedNameFromRefHub()} - TARGET SCP";
        }

        private static string GetProximitySwitchKeyString(ReferenceHub hub)
        {
            if (InputHandler.TryGetUserKey("voice_proximity", hub.characterClassManager.UserId, out var key))
                return key.ToString().SpaceByPascalCase();

            return $"<color={ColorValues.Red}>invalid key</color>";
        }

        public static bool TryGetSpectateTarget(ReferenceHub hub, out ReferenceHub target)
            => (target = GetSpectateTarget(hub)) != null;

        private static ReferenceHub GetSpectateTarget(ReferenceHub hub)
        {
            foreach (var target in ReferenceHub.AllHubs)
            {
                if (target.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (target.IsSpectatedBy(hub))
                    return target;
            }

            return null;
        }
    }
}
