using Compendium.Extensions;

using System;
using System.IO;
using System.Net;
using System.Threading;

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

        public static void Download(string target, string id, bool isDirect, Action<bool> callback = null)
        {
            if (isDirect)
            {
                new Thread(async () =>
                {
                    var path = Path.GetRandomFileName();

                    using (var web = new WebClient())
                    {
                        await web.DownloadFileTaskAsync(target, path);
                        var data = File.ReadAllBytes(path);

                        File.Delete(path);

                        AudioConverter.Convert(data, null, converted =>
                        {
                            AudioStore.Save(id, converted);
                            callback?.Invoke(true);
                        });
                    }
                }).Start();
            }
            else
            {
                AudioSearch.Find(target, null, vid =>
                {
                    if (string.IsNullOrWhiteSpace(vid.Value))
                    {
                        callback?.Invoke(false);
                        return;
                    }

                    AudioSearch.Download(vid, null, newData => AudioConverter.Convert(newData, null,
                        convertedData =>
                        {
                            AudioStore.Save(id, convertedData);
                            callback?.Invoke(true);
                        }));
                });
            }
        }
    }
}
