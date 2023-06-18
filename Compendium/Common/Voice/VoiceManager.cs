using Compendium.Helpers.Patching;
using Compendium.State;

using helpers.Extensions;

using Mirror;

using PlayerRoles.Voice;

using VoiceChat;
using VoiceChat.Networking;

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
            msg.Channel = sendChannel;

            ReferenceHub.AllHubs.ForEach(hub =>
            {
                if (hub.TryGetState<VoiceController>(out var vc))
                {
                    if (vc.Receive(speakerRole.VoiceModule.Owner, ref msg, out var shouldSend))
                    {
                        if (shouldSend)
                            hub.connectionToClient.Send(msg);

                        return;
                    }
                }

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