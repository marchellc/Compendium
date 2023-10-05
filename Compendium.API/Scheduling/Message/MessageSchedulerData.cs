using Compendium.Messages;

using System;

namespace Compendium.Scheduling.Message
{
    public struct MessageSchedulerData
    {
        public DateTime? At;

        public MessageBase Message;
        public ReferenceHub Target;

        public MessageSchedulerData(MessageBase message, ReferenceHub target, DateTime? time)
        {
            At = time;

            Message = message;
            Target = target;
        }
    }
}
