using helpers.Patching;

using Mirror;

using System;

namespace Compendium.Custom.Patches.Fixes
{
    public static class PreventDisconnectPatch
    {
        [Patch(typeof(NetworkClient), nameof(NetworkClient.OnTransportData), PatchType.Prefix)]
        public static bool OnTransportData(ArraySegment<byte> data, int channelId)
        {
            try
            {
                if (NetworkClient.connection is null)
                {
                    Plugin.Error($"Tried to receive data on a null connection, restarting the server!");
                    Shutdown.Quit(true);
                    return false;
                }

                if (!NetworkClient.unbatcher.AddBatch(data))
                {
                    Plugin.Warn($"Failed to add data to batch.");
                    return false;
                }

                while (!NetworkClient.isLoadingScene && NetworkClient.unbatcher.GetNextMessage(out var message, out var remoteTimeStamp))
                {
                    using (var reader = NetworkReaderPool.Get(message))
                    {
                        if (reader.Remaining < 2)
                        {
                            Plugin.Warn($"Received message is too short!");
                            return false;
                        }

                        NetworkClient.connection.remoteTimeStamp = remoteTimeStamp;

                        if (!NetworkClient.UnpackAndInvoke(reader, channelId))
                        {
                            Plugin.Warn($"Failed to unpack message!");
                            return false;
                        }
                    }
                }

                if (!NetworkClient.isLoadingScene && NetworkClient.unbatcher.BatchesCount > 0)
                {
                    Plugin.Warn($"There were {NetworkClient.unbatcher.BatchesCount} remaining batches.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Error($"NetworkClient.OnTransportData patch caught an exception:\n{ex}");
                return true;
            }

            return false;
        }
    }
}
