using System;
using System.Threading;

namespace Compendium.Timing
{
    public static class ThreadSafeTimer
    {
        public static Timer Create(int interval, Action callback)
            => new Timer(_ => callback(), null, 0, interval);
    }
}