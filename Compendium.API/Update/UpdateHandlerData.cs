using System;
using System.Reflection;
using System.Threading;

namespace Compendium.Update
{
    public class UpdateHandlerData
    {
        public Action Delegate { get; }
        public MethodInfo Method { get; }

        public Thread Thread { get; set; }

        public CancellationTokenSource TokenSource { get; }
        public CancellationToken Token { get; }

        public UpdateHandlerType Type { get; } = UpdateHandlerType.Thread;

        public bool SyncTickRate { get; set; }
        public bool ExecuteOnMain { get; set; }

        public int TickRate { get; set; } = 10;

        public object Handle { get; set; }

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

        public UpdateHandlerData(MethodInfo method, object handle, UpdateHandlerType type, bool syncTickRate, bool executeOnMain, int tickRate)
        {
            Method = method;
            Handle = handle;
            Type = type;
            SyncTickRate = syncTickRate;
            ExecuteOnMain = executeOnMain;
            TickRate = tickRate;

            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;
        }
    }
}