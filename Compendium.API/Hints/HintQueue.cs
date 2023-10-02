using Compendium.Events;
using Compendium.Update;

using helpers;

using Hints;

using PluginAPI.Events;

using System.Collections.Generic;
using System.Linq;

namespace Compendium.Hints
{
    public static class HintQueue
    {
        private static Dictionary<ReferenceHub, Queue<HintInfo>> _hintQueue = new Dictionary<ReferenceHub, Queue<HintInfo>>();
        private static Dictionary<ReferenceHub, HintInfo> _curHints = new Dictionary<ReferenceHub, HintInfo>();

        private static object _lock = new object();

        static HintQueue()
        {
            UpdateSynchronizer.OnUpdate += OnUpdate;
        }

        public static void Enqueue(ReferenceHub target, string message, float duration)
        {
            lock (_lock)
            {
                if (!_hintQueue.ContainsKey(target))
                    _hintQueue[target] = new Queue<HintInfo>();

                if (_hintQueue[target].Any(x => x.Message == message && x.Duration == duration))
                    return;

                _hintQueue[target].Enqueue(HintInfo.Get(message, duration));
            }
        }

        private static void OnUpdate()
        {
            lock (_lock)
            {
                foreach (var p in _hintQueue)
                {
                    if (p.Value.Count <= 0)
                        continue;

                    var peeked = p.Value.Peek();

                    if (peeked is null)
                        continue;

                    if (_curHints.ContainsKey(p.Key)
                        && _curHints[p.Key] != null)
                        continue;

                    if (!p.Value.TryDequeue(out peeked))
                        continue;

                    Show(p.Key, peeked);
                }
            }
        }

        private static void Show(ReferenceHub target, HintInfo hint)
        {
            if (!hint.IsValid())
                return;

            if (HubWorldExtensions.HintProxy != null)
                HubWorldExtensions.HintProxy(target, hint.Message, hint.Duration);
            else
                target.hints.Show(new TextHint(hint.Message, 
                    new HintParameter[] 
                    { 
                        new StringHintParameter(hint.Message) 
                    }, null, hint.Duration));

            _curHints[target] = hint;
            Calls.Delay(hint.Duration + 0.2f, () =>
            {
                _curHints[target] = null;
            });
        }

        [Event]
        private static void OnPlayerLeft(PlayerLeftEvent ev)
        {
            lock (_lock)
            {
                _curHints.Remove(ev.Player.ReferenceHub);
                _hintQueue.Remove(ev.Player.ReferenceHub);
            }
        }
    }
}
