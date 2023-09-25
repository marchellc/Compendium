using System;
using System.Threading;

namespace Compendium.Update
{
    public class UpdateHandlerData
    {
        public Action Delegate { get; }

        public Thread Thread { get; set; }

        public CancellationTokenSource TokenSource { get; }
        public CancellationToken Token { get; }

        public UpdateHandlerType Type { get; } = UpdateHandlerType.Thread;

        public bool SyncTickRate { get; set; }
        public bool ExecuteOnMain { get; set; }

        public int TickRate { get; set; } = 10;

        public UpdateHandlerData(Action del, UpdateHandlerType type, bool syncTickRate, bool executeOnMain, int tickRate)
        {
            Delegate = del;
            Type = type;
            SyncTickRate = syncTickRate;
            ExecuteOnMain = executeOnMain;
            TickRate = tickRate;

            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;
        }
    }
}