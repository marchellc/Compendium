using Compendium.Extensions;
using Compendium.Features;
using Compendium.Voice.Profiles;

using helpers.Extensions;
using helpers.Patching;

using Mirror;

using PlayerRoles;
using PlayerRoles.Spectating;
using PlayerRoles.Voice;

using System;
using System.Linq;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice
{
    public static class VoicePatch
    {
        [Patch(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage), PatchType.Prefix, "Voice Patch")]
        public static bool Prefix(NetworkConnection conn, VoiceMessage msg)
        {
            if (msg.SpeakerNull)
                return false;

            if (msg.Speaker.netId != conn.identity.netId)
                return false;

            if (!(msg.Speaker.roleManager.CurrentRole is IVoiceRole speakerRole) || speakerRole is null)
                return false;

            if (speakerRole.VoiceModule is null)
            {
                FLog.Warn($"{msg.Speaker.LoggedNameFromRefHub()}'s voice module is null!");
                return false;
            }

            if (!VoiceConfigs.BypassRateLimit)
            {
                if (!speakerRole.VoiceModule.CheckRateLimit())
                    return false;
            }
            else
            {
                // intercom fix in case rate limit bypassing is enabled
                if (speakerRole.VoiceModule is VoiceModuleBase moduleBase && moduleBase != null)
                    moduleBase._sentPackets++;
            }

            if (VoiceChatMutes.IsMuted(msg.Speaker))
                return false;

            void Send(VoiceChatChannel channel, Func<ReferenceHub, bool> condition = null)
            {
                msg.Channel = channel;
                speakerRole.VoiceModule.CurrentChannel = channel;

                ReferenceHub.AllHubs.ForEach(hub =>
                {
                    if (hub.Mode != ClientInstanceMode.ReadyClient)
                        return;

                    if (condition != null)
                    {
                        if (!condition.Invoke(hub))
                        {
                            return;
                        }
                    }

                    if (msg.Speaker.netId != hub.netId)
                    {
                        hub.connectionToClient.Send(msg);
                    }
                    else
                    {
                        if (VoiceController.CanHearSelf(hub))
                        {
                            hub.connectionToClient.Send(msg);
                        }
                    }
                });
            }

            void VanillaSend(bool exludeStaff = false)
            {
                var sendChannel = speakerRole.VoiceModule.ValidateSend(msg.Channel);

                if (sendChannel is VoiceChatChannel.None)
                    return;

                speakerRole.VoiceModule.CurrentChannel = sendChannel;

                foreach (ReferenceHub target in ReferenceHub.AllHubs)
                {
                    if (target.Mode != ClientInstanceMode.ReadyClient)
                        continue;

                    if (!(target.roleManager.CurrentRole is IVoiceRole recvRole) || recvRole is null)
                        continue;

                    if (recvRole.VoiceModule is null)
                        continue;

                    if (exludeStaff && target.serverRoles.RemoteAdmin)
                        continue;

                    var recvChannel = recvRole.VoiceModule.ValidateReceive(msg.Speaker, sendChannel);

                    if (recvChannel != VoiceChatChannel.None)
                    {
                        msg.Channel = recvChannel;
                        target.connectionToClient.Send(msg);
                    }
                }
            }

            if (VoiceController.PriorityVoice != null)
            {
                if (VoiceController.StaffFlags != StaffVoiceFlags.None)
                {
                    if (VoiceController.StaffFlags is StaffVoiceFlags.AllowNonStaffListen)
                    {
                        if (!msg.Speaker.serverRoles.RemoteAdmin)
                        {
                            VanillaSend(true);
                            return false;
                        }
                        else
                        {
                            Send(VoiceChatChannel.RoundSummary, hub => true);
                            return false;
                        }
                    }
                    else
                    {
                        if (!msg.Speaker.serverRoles.RemoteAdmin)
                        {
                            VanillaSend(true);
                            return false;
                        }
                        else
                        {
                            Send(VoiceChatChannel.RoundSummary, hub => hub.serverRoles.RemoteAdmin);
                            return false;
                        }
                    }
                }
                else
                {
                    if (msg.Speaker.netId != VoiceController.PriorityVoice.netId)
                        return false;

                    Send(VoiceChatChannel.RoundSummary, _ => true);
                    return false;
                }
            }

            if (VoiceController.TryGetProfile(msg.Speaker, out var profile) && profile is ScpVoiceProfile scpProfile)
            {
                if (scpProfile.IsProximityActive)
                {
                    ReferenceHub.AllHubs.ForEach(hub =>
                    {
                        if (hub.Mode != ClientInstanceMode.ReadyClient)
                            return;

                        if (hub.netId == msg.Speaker.netId && !VoiceController.CanHearSelf(hub))
                            return;

                        if (hub.GetRoleId() is RoleTypeId.Overwatch && VoiceConfigs.AllowOverwatchScpChat)
                        {
                            if (!VoiceController.m_OvFlags.TryGetValue(hub.netId, out var flags))
                                flags = OverwatchVoiceFlags.TargetScp;

                            if ((flags is OverwatchVoiceFlags.TargetScp && msg.Speaker.IsSpectatedBy(hub)) || (flags is OverwatchVoiceFlags.AllScps && ReferenceHub.AllHubs.Any(target =>
                            {
                                if (target.Mode != ClientInstanceMode.ReadyClient)
                                    return false;

                                if (!target.IsSCP())
                                    return false;

                                return target.IsSpectatedBy(hub);
                            })))
                            {
                                msg.Channel = VoiceChatChannel.RoundSummary;
                                speakerRole.VoiceModule.CurrentChannel = VoiceChatChannel.RoundSummary;

                                hub.connectionToClient.Send(msg);
                                return;
                            }
                        }

                        if (hub.IsSCP())
                        {
                            msg.Channel = VoiceChatChannel.ScpChat;
                            speakerRole.VoiceModule.CurrentChannel = VoiceChatChannel.ScpChat;

                            hub.connectionToClient.Send(msg);
                            return;
                        }

                        if (!VoiceConfigs.ProximityScps.Contains(msg.Speaker.GetRoleId()))
                            return;

                        if (hub.IsAlive())
                        {
                            if (hub.IsWithinDistance(msg.Speaker, VoiceConfigs.ScpProximityDistance))
                            {
                                msg.Channel = VoiceConfigs.ProximityChannel;
                                speakerRole.VoiceModule.CurrentChannel = VoiceConfigs.ProximityChannel;

                                hub.connectionToClient.Send(msg);
                            }    
                        }
                    });

                    return false;
                }
            }

            VanillaSend();
            return false;
        }
    }
}
