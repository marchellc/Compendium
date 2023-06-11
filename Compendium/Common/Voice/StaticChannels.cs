using Compendium.Common.Voice.Channels;

using System.Collections.Generic;

namespace Compendium.Common.Voice
{
    public static class StaticChannels
    {
        public static readonly IVoiceChannel ScpChannel = new ScpChannel();
        public static readonly IVoiceChannel AdminChannel = new AdminChannel();
        public static readonly IVoiceChannel ProximityChannel = new ProximityChannel();

        public static IReadOnlyCollection<int> ReservedIds { get; } = new List<int>() { 99, 50, 45, 40, 35, 30 };
    }
}