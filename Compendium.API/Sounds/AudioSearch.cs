using helpers.Extensions;
using helpers.Random;
using helpers.Time;

using System;
using System.IO;
using System.Linq;
using System.Threading;

using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Compendium.Sounds
{
    public static class AudioSearch
    {
        private static YoutubeClient _yt = new YoutubeClient();

        public static void Find(string query, Action<string> message, Action<VideoId> callback)
        {
            new Thread(async () =>
            {
                message?.Invoke($"Searching for query: '{query}'");

                foreach (var result in await _yt.Search.GetResultsAsync(query).CollectAsync())
                {
                    if (result is VideoSearchResult videoSearch)
                    {
                        message?.Invoke($"Found result: '{videoSearch.Title}' (by '{videoSearch.Author}') [{videoSearch.Duration.GetValueOrDefault().UserFriendlySpan()}]");

                        callback?.Invoke(videoSearch.Id);
                        return;
                    }
                }

                callback?.Invoke(default);

                message?.Invoke($"Failed to find any results for your query!");
            }).Start();
        }

        public static void Download(VideoId video, Action<string> message, Action<byte[]> result)
        {
            new Thread(async () =>
            {
                try
                {
                    message?.Invoke($"Retrieving streaming manifest ..");

                    var streams = await _yt.Videos.Streams.GetManifestAsync(video);
                    var validStreams = streams.GetAudioStreams();

                    message?.Invoke($"Found {validStreams.Count()} audio stream(s).");

                    if (!validStreams.Any())
                    {
                        message?.Invoke($"Failed to find a valid audio stream!");

                        result?.Invoke(null);
                        return;
                    }

                    var selectedStream = validStreams.OrderByDescending(a => a.Bitrate.BitsPerSecond).First();
                    var id = RandomGeneration.Default.GetReadableString(20).RemovePathUnsafe().Replace("/", "");
                    var tempPath = $"{AudioStore.DirectoryPath}/{id}";

                    message?.Invoke($"Selected audio stream: {selectedStream.AudioCodec} ({selectedStream.Bitrate.BitsPerSecond} b/s)");
                    message?.Invoke($"Downloading ..");

                    await _yt.Videos.Streams.DownloadAsync(selectedStream, tempPath);

                    var data = File.ReadAllBytes(tempPath);

                    File.Delete(tempPath);

                    message?.Invoke($"Downloaded {data.Length} bytes!");
                    result?.Invoke(data);
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex);
                }
            }).Start(); 
        }
    }
}