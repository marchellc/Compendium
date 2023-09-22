using Compendium.Extensions;

using helpers;

using PlayerRoles;
using PlayerRoles.Spectating;

using System;

using UnityEngine;

namespace Compendium.Hints
{
    public class HintInfo
    {
        public string Message { get; set; } = "";
        public float Duration { get; set; } = 0f;

        public BasicHintEffectType[] Effects { get; set; } = Array.Empty<BasicHintEffectType>();

        public void Send(ReferenceHub target)
        {
            if (string.IsNullOrWhiteSpace(Message) || Duration <= 0)
                return;

            target.Hint(Message, Duration, false, Effects);
        }

        public void SendToAll()
        {
            if (string.IsNullOrWhiteSpace(Message) || Duration <= 0)
                return;

            World.Hint(Message, Duration, false, Effects);
        }

        public void SendToSpectatorsOf(ReferenceHub hub)
        {
            if (string.IsNullOrWhiteSpace(Message) || Duration <= 0)
                return;

            Hub.Hubs.ForEach(h =>
            {
                if (!h.IsPlayer() || h.IsAlive() || h == hub)
                    return;

                if (!hub.IsSpectatedBy(hub))
                    return;

                h.Hint(Message, Duration, false, Effects);
            });
        }

        public void SendRange(Vector3 position, float range)
        {
            if (string.IsNullOrWhiteSpace(Message) || Duration <= 0)
                return;

            Hub.Hubs.ForEach(h =>
            {
                if (!h.IsPlayer() || h.IsAlive())
                    return;

                if (!h.IsWithinDistance(position, range))
                    return;

                h.Hint(Message, Duration, false, Effects);
            });
        }

        public static HintInfo Get(object message, float duration = 3f, params BasicHintEffectType[] effects)
            => new HintInfo
            {
                Message = message?.ToString() ?? "",
                Duration = duration,
                Effects = effects
            };
    }
}