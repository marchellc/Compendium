using Mirror;

using System;

namespace Compendium.Npc
{
    public class NpcConnection : NetworkConnectionToClient
    {
        public NpcConnection(int networkConnectionId) : base(networkConnectionId) { }

        public override string address => "localhost";
        public override void Send(ArraySegment<byte> segment, int channelId = 0) { }
    }
}