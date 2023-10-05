using Compendium.Messages;
using Compendium.Scheduling.Message;
using Compendium.Timing;

using helpers;
using helpers.Attributes;

using System;
using System.Collections.Generic;

namespace Compendium.Scheduling
{
    public static class MessageScheduler
    {
        private static readonly List<MessageSchedulerData> _messageList = new List<MessageSchedulerData>();
        private static readonly object _lock = new object();

        [Load]
        private static void Load()
            => ThreadSafeTimer.Create(10, OnTick);

        public static void Schedule(ReferenceHub target, MessageBase message, int? msDelay = null)
        {
            lock (_lock)
            {
                if (msDelay.HasValue)
                    _messageList.Add(new MessageSchedulerData(message, target, DateTime.Now + TimeSpan.FromMilliseconds(msDelay.Value)));
                else
                    _messageList.Add(new MessageSchedulerData(message, target, null));
            }
        }

        private static void OnTick()
        {
            lock (_lock)
            {
                if (_messageList.Count > 0)
                {
                    var sent = Pools.PoolList<MessageSchedulerData>();

                    for (int i = 0; i < _messageList.Count; i++)
                    {
                        var data = _messageList[i];

                        if (data.At.HasValue && DateTime.Now < data.At.Value)
                            continue;

                        data.Message.Send(data.Target);
                        sent.Add(data);
                    }

                    sent.For((_, data) => _messageList.Remove(data));
                    sent.ReturnList();
                }
            }
        }
    }
}