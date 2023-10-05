using Compendium.Conditions;

using helpers;
using helpers.Values;

using System;
using System.Linq;

namespace Compendium.Messages
{
    public class MessageBase : IValue<string>
    {
        public string Value { get; set; }
        public double Duration { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value) && Duration > 0;

        public virtual void Send(ReferenceHub hub) { }

        public void SendToTargets(params ReferenceHub[] targets) => targets.ForEach(Send);
        public void SendToAll() => Hub.Hubs.ForEach(Send);

        public void SendConditionally(Predicate<ReferenceHub> predicate) => Hub.ForEach(Send, predicate);
        public void SendConditionally(params Condition[] conditions) => Hub.ForEach(Send, hub => conditions.All(c => c.IsMatch(hub)));
    }
}