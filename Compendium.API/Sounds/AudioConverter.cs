using helpers.Extensions;
using helpers.Random;

using System;
using System.IO;
using System.Linq;
using System.Threading;

using Xabe.FFmpeg;

namespace Compendium.Sounds
{
    public static class AudioConverter
    {
        public static void Convert(byte[] data, Action<string> message, Action<byte[]> result)
        {
            var sessionId = RandomGeneration.Default.GetReadableString(20).RemovePathUnsafe().Replace("/", "");
            var sourcePath = $"{AudioStore.DirectoryPath}/{sessionId}";
            var destPath = $"{AudioStore.DirectoryPath}/{sessionId}.ogg";

            new Thread(async () =>
            {
                try
                {
                    File.WriteAllBytes(sourcePath, data);

                    var mediaInfo = await FFmpeg.GetMediaInfo(sourcePath);

                    message?.Invoke($"Retrieving audio streams ..");

                    var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

                    message?.Invoke($"Chosen stream: {audioStream.Codec} '{audioStream.Bitrate} kb/s'");
                    message?.Invoke($"Converting ..");

                    var conversion = FFmpeg.Conversions.New()
                        .AddStream(audioStream)
                        .AddParameter("-vn")
                        .AddParameter("-acodec libvorbis")
                        .AddParameter("-ac 1")
                        .AddParameter("-ar 48000")
                        .AddParameter($"-b:a 120k")
                        .SetOutputFormat(Format.ogg)
                        .SetOutput(destPath);

                    var convResult = await conversion.Start();
                    var resultData = File.ReadAllBytes(destPath);

                    message?.Invoke($"Conversion finished!");

                    File.Delete(sourcePath);
                    File.Delete(destPath);

                    result?.Invoke(resultData);
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex);
                }
            }).Start();

            Plugin.Debug($"Conversion thread {sessionId} started");
        }
    }
}