using Compendium;
using Compendium.Colors;
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
            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", OnUpdate);
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

                if (VoiceController.PriorityVoice != null)
                {
                    if (VoiceController.StaffFlags is StaffVoiceFlags.None)
                    {
                        if (hub.netId == VoiceController.PriorityVoice.netId)
                        {
                            hub.Hint(
                                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Global Voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                                $"<b>Only <color={ColorValues.Red}>you</color> can speak.</b></size>", 6.1f, true);
                            continue;
                        }
                        else
                        {
                            hub.Hint(
                                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Global Voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                                $"<b>Only <color={ColorValues.Red}>{VoiceController.PriorityVoice.nicknameSync.Network_myNickSync}</color> can speak.</b></color>", 6.1f, true);
                            continue;
                        }
                    }
                    else
                    {
                        if (hub.netId == VoiceController.PriorityVoice.netId)
                        {
                            hub.Hint(
                                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Staff Voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                                $"<b>Only <color={ColorValues.Red}>you</color> can control it.</b>\n" +
                                $"<b>Use the <color={ColorValues.Green}>staffmode</color> command to allow/disallow other players to listen.</b></size>", 6.1f, true);
                            continue;
                        }
                        else
                        {
                            hub.Hint(
                                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Staff Voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                                $"<b>Only <color={ColorValues.Red}>{VoiceController.PriorityVoice.nicknameSync.Network_myNickSync}</color> can control it.</b></size>", 6.1f, true);
                            continue;
                        }
                    }
                }

                if (VoiceController.CanHearSelf(hub))
                {
                    hub.Hint(
                        $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Microphone Playback</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                        $"<b>Use the <color={ColorValues.Red}>.playback</color> command to disable it.</b></sze>", 6.1f, true);
                    continue;
                }

                if (VoiceConfigs.AllowOverwatchScpChat
                    && hub.GetRoleId() is RoleTypeId.Overwatch
                    && TryGetSpectateTarget(hub, out var target)
                    && target.IsSCP())
                {
                    UpdateOverwatchHint(hub, target);
                    continue;
                }

                if (VoiceController.TryGetProfile(hub, out var profile) 
                    && profile is ScpVoiceProfile voiceProfile)
                {
                    if (voiceProfile.ProximityFlag is ProximityVoiceFlags.Single)
                    {
                        hub.Hint(
                            $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Proximity voice</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                            $"Use <color={ColorValues.Red}>{GetProximitySwitchKeyString(hub)}</color> to switch to <b><color={ColorValues.Red}>{GetNextChat(voiceProfile.ProximityFlag)}</color></b>.</size>", 6.1f, true);
                        continue;
                    }
                    else if (voiceProfile.ProximityFlag is ProximityVoiceFlags.Combined)
                    {
                        hub.Hint(
                            $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Proximity voice</color> & <color={ColorValues.LightGreen}>SCP chat</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                            $"Use <color={ColorValues.Red}>{GetProximitySwitchKeyString(hub)}</color> to switch to <b><color={ColorValues.Red}>{GetNextChat(voiceProfile.ProximityFlag)}</color></b>.</size>", 6.1f, true);
                        continue;
                    }
                    else if (voiceProfile.ProximityFlag is ProximityVoiceFlags.Inactive && voiceProfile.IsProximityAvailable())
                    {
                        hub.Hint(
                            $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>SCP chat</color> is <color={ColorValues.Green}>active</color>.</b>\n" +
                            $"Use <color={ColorValues.Red}>{GetProximitySwitchKeyString(hub)}</color> to switch to <b><color={ColorValues.Red}>{GetNextChat(voiceProfile.ProximityFlag)}</color></b>.</size>", 6.1f, true);
                        continue;
                    }

                    continue;
                }
            }
        }

        private static void UpdateOverwatchHint(ReferenceHub hub, ReferenceHub target)
            => hub.Hint(
                $"\n\n\n\n\n\n\n\n\n\n<size=17><b><color={ColorValues.LightGreen}>Overwatch</color> allows you to hear other SCPs talk.</b>\n" +
                $"Use the <b><color={ColorValues.Red}>ovmode</color></b> command to switch between listening to all SCPs and targeted SCPs.\n" +
                $"\n<b><i>Current target: {GetOvTargetString(hub, target)}</i></b></size>", 6.1f, true);

        private static string GetOvTargetString(ReferenceHub hub, ReferenceHub target)
        {
            if (!VoiceController.OverwatchFlags.TryGetValue(hub.netId, out var flags))
                flags = OverwatchVoiceFlags.TargetScp;

            if (flags is OverwatchVoiceFlags.AllScps)
                return $"{target.LoggedNameFromRefHub()} - ALL SCPS";
            else
                return $"{target.LoggedNameFromRefHub()} - TARGET SCP";
        }

        public static string GetProximitySwitchKeyString(ReferenceHub hub)
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

        private static string GetNextChat(ProximityVoiceFlags proximityVoiceFlags)
        {
            if (proximityVoiceFlags is ProximityVoiceFlags.Inactive)
                return "Proximity chat.";
            else if (proximityVoiceFlags is ProximityVoiceFlags.Single)
                return "Proximity chat & SCP chat";
            else
                return "SCP chat";
        }
    }
}
