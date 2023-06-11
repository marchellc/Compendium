using PlayerRoles.Voice;

using System.Collections.Generic;

using VoiceChat;
using VoiceChat.Networking;

namespace Compendium.Common.Voice
{
    public interface IVoiceChannel
    {
        string Name { get; }

        int Id { get; }

        VoiceChatChannel Channel { get; }

        IReadOnlyCollection<ReferenceHub> Members { get; }

        bool CanJoin(ReferenceHub hub);
        bool CanReceive(IVoiceRole speakerRole, ReferenceHub receiver);

        bool Contains(ReferenceHub hub);

        void Receive(IVoiceRole speakerRole, ReferenceHub receiver, VoiceMessage voiceMessage);

        void Join(ReferenceHub hub);
        void Leave(ReferenceHub hub);
    }
}