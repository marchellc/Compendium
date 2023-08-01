using helpers.Pooling;
using helpers.Pooling.Pools;

using VoiceChat;

namespace Compendium.Voice.Pools
{
    public class PacketPool : Pool<VoicePacket>
    {
        public static PacketPool Pool { get; } = new PacketPool();

        public PacketPool() : base(PrepareGet, PrepareStore, Constructor) { }

        private static void PrepareGet(VoicePacket packet)
        {
            packet.Destinations = DictionaryPool<ReferenceHub, VoiceChatChannel>.Pool.Get();
            packet.SenderChannel = default;
            packet.Role = null;
            packet.Module = null;
            packet.Speaker = null;
        }

        private static void PrepareStore(VoicePacket packet)
        {
            DictionaryPool<ReferenceHub, VoiceChatChannel>.Pool.Push(packet.Destinations);
            packet.SenderChannel = default;
            packet.Role = null;
            packet.Destinations = null;
            packet.Module = null;
            packet.Speaker = null;
        }

        private static VoicePacket Constructor() => new VoicePacket();
    }
}