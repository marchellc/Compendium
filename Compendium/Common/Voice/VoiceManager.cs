using Compendium.Helpers.Patching;
using Compendium.State;

using helpers.Extensions;

using Mirror;

using PlayerRoles.Voice;

using VoiceChat;
using VoiceChat.Networking;

using BetterCommands.Management;

using UnityEngine;

using Command = BetterCommands.CommandAttribute;

namespace Compendium.Common.Voice
{
    public static class VoiceManager
    {
        public static readonly PatchData VoicePatchData = PatchData.New()
            .WithType(PatchType.Prefix)
            .WithTarget(typeof(VoiceTransceiver), nameof(VoiceTransceiver.ServerReceiveMessage))
            .WithReplacement(typeof(VoiceManager), nameof(VoiceManager.VoicePatch))
            .WithName("Voice Patch");

        static VoiceManager()
        {
            PatchManager.ApplyPatch(VoicePatchData);
        }

        [@Command("voice", CommandType.PlayerConsole)]
        public static void SwitchCommand(ReferenceHub sender)
        {
            if (sender.TryGetState<VoiceController>(out var vc))
            {
                vc.SwitchKey(KeyCode.LeftAlt, sender, null);
            }
        }

        private static bool VoicePatch(NetworkConnection conn, VoiceMessage msg)
        {
            if (msg.SpeakerNull || msg.Speaker.netId != conn.identity.netId)
                return false;

            if (!(msg.Speaker.roleManager.CurrentRole is IVoiceRole speakerRole))
                return false;

            if (!speakerRole.VoiceModule.CheckRateLimit())
                return false;

            if (VoiceChatMutes.IsMuted(msg.Speaker))
                return false;

            var sendChannel = speakerRole.VoiceModule.ValidateSend(msg.Channel);
            if (sendChannel is VoiceChatChannel.None)
                return false;

            speakerRole.VoiceModule.CurrentChannel = sendChannel;

            if (sendChannel != VoiceChatChannel.None 
                && sendChannel != VoiceChatChannel.Mimicry 
                && sendChannel != VoiceChatChannel.Radio
                && sendChannel != VoiceChatChannel.RoundSummary
                && sendChannel != VoiceChatChannel.Scp1576
                && sendChannel != VoiceChatChannel.Spectator
                && sendChannel != VoiceChatChannel.Intercom)
            {
                if (msg.Speaker.TryGetState<VoiceController>(out var vcController))
                {
                    if (vcController.Receive(speakerRole, msg, sendChannel))
                    {
                        return false;
                    }
                }
            }

            ReferenceHub.AllHubs.ForEach(hub =>
            {
                if (!(hub.roleManager.CurrentRole is IVoiceRole recvRole))
                    return;

                var recvChannel = recvRole.VoiceModule.ValidateReceive(msg.Speaker, sendChannel);
                if (recvChannel != VoiceChatChannel.None)
                {
                    msg.Channel = recvChannel;
                    hub.connectionToClient.Send(msg);
                }
            }, hub => hub.Mode is ClientInstanceMode.ReadyClient);

            return false;
        }
    }
}