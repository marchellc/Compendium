using PlayerRoles.Voice;

using System.Collections.Generic;

using VoiceChat;

namespace Compendium.Voice
{
    public class VoicePacket
    {
        public ReferenceHub Speaker { get; set; }
        public VoiceModuleBase Module { get; set; }

        public IVoiceRole Role { get; set; }

        public VoiceChatChannel SenderChannel { get; set; }

        public Dictionary<ReferenceHub, VoiceChatChannel> Destinations { get; set; }
        public Dictionary<ReferenceHub, ReferenceHub> AlternativeSenders { get; set; }
    }
}