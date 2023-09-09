using Compendium.Extensions;

using VoiceChat;

namespace Compendium.Sounds
{
    public static class AudioUtils
    {
        public static bool ValidateChannelMode(VoiceChatChannel channel, VoiceChatChannel mode, ReferenceHub receiver, ReferenceHub speaker, float distance)
        {
            if (mode is VoiceChatChannel.Proximity)
                return receiver.IsWithinDistance(speaker, distance);

            return true;
        }
    }
}
