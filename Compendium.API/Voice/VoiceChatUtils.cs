using Compendium.Voice.Pools;
using Compendium.Voice.States;
using Compendium.Voice.States.StaffVoice;

using helpers.Enums;

using PlayerRoles.Voice;

using System.Collections.Generic;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Voice
{
    public static class VoiceChatUtils
    {
        public static bool CheckRateLimit(VoiceModuleBase module, bool addPacket = true)
        {
            if (addPacket)
                module._sentPackets++;

            if (Plugin.Config.VoiceSettings.CustomRateLimit != 0 && module._sentPackets > Plugin.Config.VoiceSettings.CustomRateLimit)
                return false;

            return true;
        }

        public static bool CanHearSelf(ReferenceHub hub)
        {
            var modifiers = VoiceChat.GetModifiers(hub);

            if (modifiers.HasValue && modifiers.Value.HasFlagFast(VoiceModifier.PlaybackEnabled))
                return true;

            return false;
        }

        public static void MakeGlobalSpeaker(ReferenceHub hub)
            => VoiceChat.State = new GlobalVoiceState(hub);

        public static void MakeStaffSpeaker(ReferenceHub hub)
            => VoiceChat.State = new StaffVoiceState(hub);

        public static ReferenceHub GetGlobalSpeaker()
        {
            if (VoiceChat.State != null && VoiceChat.State is GlobalVoiceState globalVoice)
                return globalVoice.Starter;

            return null;
        }

        public static ReferenceHub GetStaffSpeaker()
        {
            if (VoiceChat.State != null && VoiceChat.State is StaffVoiceState staffVoice)
                return staffVoice.Starter;

            return null;
        }

        public static void EndCurrentState()
            => VoiceChat.State = null;

        public static VoicePacket GeneratePacket(VoiceMessage message, IVoiceRole speakerRole, VoiceChatChannel origChannel)
        {
            var packet = PacketPool.Pool.Get();

            packet.SenderChannel = origChannel;
            packet.Role = speakerRole;
            packet.Module = speakerRole.VoiceModule;
            packet.Speaker = message.Speaker;

            GenerateDestinations(message, origChannel, packet.Destinations);

            return packet;
        }

        public static void GenerateDestinations(VoiceMessage message, VoiceChatChannel origChannel, Dictionary<ReferenceHub, VoiceChatChannel> dict)
        {
            Hub.ForEach(hub =>
            {
                if (hub.netId == message.Speaker.netId && !CanHearSelf(hub))
                {
                    dict[hub] = VoiceChatChannel.None;
                    return;
                }

                if (!(hub.roleManager.CurrentRole is IVoiceRole vcRole))
                {
                    dict[hub] = VoiceChatChannel.None;
                    return;
                }

                dict[hub] = vcRole.VoiceModule.ValidateReceive(message.Speaker, origChannel);
            });
        }
    }
}
