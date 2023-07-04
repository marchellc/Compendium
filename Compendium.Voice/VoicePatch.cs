using Compendium.Features;

using helpers.Extensions;
using helpers.Patching;

using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Voice;
using PluginAPI.Core;
using RelativePositioning;
using Respawning.NamingRules;
using System;
using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice
{
    public static class VoicePatch
    {
        public static readonly PatchInfo patch = new PatchInfo(
            new PatchTarget(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage)),
            new PatchTarget(typeof(VoicePatch), nameof(VoicePatch.Prefix)), PatchType.Prefix, "Voice Patch");

        public static void Apply() => PatchManager.Patch(patch);
        public static void Unapply() => PatchManager.Unpatch(patch);

        public static bool IsIcomSpeaker(ReferenceHub hub)
        {
            if (Intercom._singleton is null)
                return false;

            if (Intercom._singleton._curSpeaker is null)
                return false;

            return Intercom._singleton._curSpeaker.netId == hub.netId;
        }

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

            if (VoiceController.GlobalVoiceFlags != GlobalVoiceFlags.None)
            {
                if (VoiceController.GlobalVoiceFlags is GlobalVoiceFlags.SpeakerOnly)
                {
                    if (VoiceController.GlobalSpeaker != null)
                    {
                        if (VoiceController.GlobalSpeaker.netId == msg.Speaker.netId)
                        {
                            ReferenceHub.AllHubs.ForEach(hub =>
                            {
                                if (hub.Mode != ClientInstanceMode.ReadyClient)
                                    return;

                                if (hub.netId == msg.Speaker.netId)
                                    return;

                                msg.Channel = VoiceChatChannel.RoundSummary;
                                speakerRole.VoiceModule.CurrentChannel = VoiceChatChannel.RoundSummary;
                                hub.connectionToClient.Send(msg);
                            });

                            return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else if (VoiceController.GlobalVoiceFlags is GlobalVoiceFlags.StaffOnly)
                {
                    if (msg.Speaker.serverRoles.RemoteAdmin)
                    {
                        ReferenceHub.AllHubs.ForEach(hub =>
                        {
                            if (hub.Mode != ClientInstanceMode.ReadyClient)
                                return;

                            if (hub.netId == msg.Speaker.netId)
                                return;

                            msg.Channel = VoiceChatChannel.RoundSummary;
                            speakerRole.VoiceModule.CurrentChannel = VoiceChatChannel.RoundSummary;
                            hub.connectionToClient.Send(msg);
                        });

                        return false;
                    }

                    return false;
                }
            }

            var sendChannel = speakerRole.VoiceModule.ValidateSend(msg.Channel);

            if (VoiceController.IsActive && VoiceController.CanBeHandled(msg.Channel)
                && !IsIcomSpeaker(msg.Speaker))
            {
                VoiceController.HandleMessage(msg.Speaker, speakerRole, msg);
            }
            else
            {
                if (sendChannel is VoiceChatChannel.None)
                    return false;

                speakerRole.VoiceModule.CurrentChannel = sendChannel;

                foreach (ReferenceHub target in ReferenceHub.AllHubs)
                {
                    if (target.Mode != ClientInstanceMode.ReadyClient)
                        continue;

                    if (!(target.roleManager.CurrentRole is IVoiceRole recvRole) || recvRole is null)
                        continue;

                    if (recvRole.VoiceModule is null)
                        continue;

                    var recvChannel = recvRole.VoiceModule.ValidateReceive(msg.Speaker, sendChannel);

                    if (recvChannel != VoiceChatChannel.None)
                    {
                        msg.Channel = recvChannel;
                        target.connectionToClient.Send(msg);
                    }
                }
            }

            return false;
        }
    }
}
