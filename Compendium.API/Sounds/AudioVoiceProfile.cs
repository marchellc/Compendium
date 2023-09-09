using Compendium.Extensions;
using Compendium.Voice;
using Compendium.Voice.Profiles;

using helpers.Extensions;
using helpers.Pooling.Pools;

using VoiceChat;

namespace Compendium.Sounds
{
    public class AudioVoiceProfile : BaseProfile
    {
        private AudioPlayer _player;

        public AudioVoiceProfile(ReferenceHub owner, AudioPlayer player) : base(owner) { _player = player; }

        public override void Process(VoicePacket packet)
        {
            base.Process(packet);

            packet.SenderChannel = _player.Channel;

            var destinations = DictionaryPool<ReferenceHub, VoiceChatChannel>.Pool.Get(packet.Destinations);

            packet.Destinations.ForEach(pair =>
            {
                if (!destinations.ContainsKey(pair.Key))
                    return;

                if (pair.Key == packet.Speaker)
                {
                    destinations[pair.Key] = _player.Channel;
                    return;
                }

                if (_player.Channel is VoiceChatChannel.Proximity || _player.ChannelMode is VoiceChatChannel.Proximity)
                {
                    if (pair.Key.IsWithinDistance(packet.Speaker, _player.Distance))
                    {
                        destinations[pair.Key] = _player.Channel;
                        return;
                    }
                    else
                    {
                        destinations[pair.Key] = VoiceChatChannel.None;
                        return;
                    }
                }

                destinations[pair.Key] = _player.Channel;
                return;
            });

            packet.Destinations.Clear();
            packet.Destinations.AddRange(destinations);

            DictionaryPool<ReferenceHub, VoiceChatChannel>.Pool.Push(destinations);
        }
    }
}