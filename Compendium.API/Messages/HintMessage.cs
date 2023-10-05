using Hints;

using System;

namespace Compendium.Messages
{
    public class HintMessage : MessageBase
    {
        public static event Action<HintMessage, ReferenceHub> HintProxies;

        public override void Send(ReferenceHub hub)
        {
            if (HintProxies != null)
                HintProxies.Invoke(this, hub);
            else
            {
                var textParam = new StringHintParameter(Value);
                var hintParams = new HintParameter[] { textParam };
                var textHint = new TextHint(Value, hintParams, null, (float)Duration);

                hub.hints.Show(textHint);
            }
        }

        public static HintMessage Create(string content, double duration)
            => new HintMessage
            {
                Duration = duration,
                Value = content
            };
    }
}