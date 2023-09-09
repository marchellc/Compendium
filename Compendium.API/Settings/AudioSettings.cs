using System.Collections.Generic;
using System.ComponentModel;

using VoiceChat.Codec.Enums;

namespace Compendium.Settings
{
    public class AudioSettings
    {
        public AudioSettings()
        {
            PreloadIds = new List<string>();

            HeadSamples = 1920;
            SamplingRate = 48000;

            SendBufferSize = SamplingRate / 5 + HeadSamples;
            ReadBufferSize = SamplingRate / 5 + HeadSamples;

            EncodingBufferSize = 512;

            OpusType = OpusApplicationType.Voip;
        }

        [Description("A list of audio IDs to preload.")]
        public List<string> PreloadIds { get; set; }

        [Description("Number of head sumples.")]
        public int HeadSamples { get; set; }

        [Description("Audio sample rate.")]
        public int SamplingRate { get; set; }

        [Description("Size of the sending buffer.")]
        public int SendBufferSize { get; set; }

        [Description("Size of the reading buffer.")]
        public int ReadBufferSize { get; set; }

        [Description("Size of the encoding buffer.")]
        public int EncodingBufferSize { get; set; }

        [Description("Opus encoder settings.")]
        public OpusApplicationType OpusType { get; set; } 
    }
}