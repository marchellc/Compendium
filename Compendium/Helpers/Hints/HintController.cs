using Compendium.State.Base;

using helpers.Extensions;

using Hints;

using MEC;

using PluginAPI.Core;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Helpers.Hints
{
    public class HintController : RequiredStateBase
    {
        private static HashSet<Hint> m_Global = new HashSet<Hint>();

        private static Hint m_GlobalForced;
        private static Hint m_GlobalOverride;

        private Hint m_Current;
        private Hint m_Forced;
        private Hint m_Override;

        private HashSet<Hint> m_Personal = new HashSet<Hint>();
        private readonly ConcurrentQueue<Hint> m_Queue = new ConcurrentQueue<Hint>();

        private bool m_IsShowing;

        public bool IsShowing => m_IsShowing;

        public Hint Current => m_Current;
        public Hint Forced { get => m_Forced; set => m_Forced = value; }
        public Hint Override { get => m_Override; set => m_Override = value; }

        public static Hint GlobalForced { get => m_GlobalForced; set => m_GlobalForced = value; }
        public static Hint GlobalOverride { get => m_GlobalOverride; set => m_GlobalOverride = value; }

        public override string Name => "Hint Display";

        public void Add(Hint hint)
        {
            m_Personal.Add(hint);
            m_Personal = m_Personal.OrderByDescending(x => (int)x.Priority).ToHashSet();

            Log.Debug($"Added hint", "Hint Controller");
        }

        public void Remove(Hint hint)
        {
            m_Personal.Remove(hint);
            m_Personal = m_Personal.OrderByDescending(x => (int)x.Priority).ToHashSet();

            if (m_Override != null && m_Override == hint)
                m_Override = null;

            if (m_Forced != null && m_Forced == hint)
                m_Forced = null;
        }

        public void Clear()
        {
            m_Personal.Clear();
            m_Forced = null;
            m_Override = null;
        }

        public static void AddGlobal(Hint hint)
        {
            m_Global.Add(hint);
            m_Global = m_Global.OrderByDescending(x => (int)x.Priority).ToHashSet();
        }

        public static void RemoveGlobal(Hint hint)
        {
            m_Global.Remove(hint);
            m_Global = m_Global.OrderByDescending(x => (int)x.Priority).ToHashSet();

            if (m_GlobalOverride != null && m_GlobalOverride == hint)
                m_GlobalOverride = null;

            if (m_GlobalForced != null && m_GlobalForced == hint)
                m_GlobalForced = null;
        }

        public static void ClearGlobal()
        {
            m_Global.Clear();
            m_GlobalForced = null;
            m_GlobalOverride = null;
        }

        public void Display(Hint hint)
        {
            Log.Debug($"Showing hint", "Hint Controller");

            m_IsShowing = true;
            m_Current = hint;

            var baseDuration = hint.Duration;

            Log.Debug($"Base duration: {baseDuration}", "Hint Controller");

            hint.Show(Player);

            foreach (var effect in hint.Effects)
            {
                baseDuration += effect.DurationScalar;
                
                if (effect is AlphaEffect alpha)
                    baseDuration += alpha.Alpha;
            }

            Log.Debug($"Full duration: {baseDuration}", "Hint Controller");

            Timing.CallDelayed(baseDuration, () =>
            {
                m_IsShowing = false;
                m_Current = null;

                Log.Debug($"Hint expired", "Hint Controller");
            });
        }

        public override void OnUpdate()
        {
            if (m_IsShowing)
                return;

            Log.Debug($"OnUpdate: {m_Forced != null} {m_GlobalForced != null} {m_Override != null} {m_GlobalOverride != null} {m_Queue.Count}", "Hint Controller");

            if (m_Forced != null)
            {
                if (m_Forced.CanShow(Player))
                {
                    Display(m_Forced);
                    m_Forced = null;
                    return;
                }
            }

            if (m_GlobalForced != null)
            {
                if (m_GlobalForced.CanShow(Player))
                {
                    Display(m_GlobalForced);
                    m_GlobalForced = null;
                    return;
                }
            }

            if (m_Override != null)
            {
                if (m_Override.CanShow(Player))
                {
                    Display(m_Override);
                    return;
                }
            }

            if (m_GlobalOverride != null)
            {
                if (m_GlobalOverride.CanShow(Player))
                {
                    Display(m_GlobalOverride);
                    return;
                }
            }

            if (m_Queue.IsEmpty)
            {
                m_Personal.ForEach(hint =>
                {
                    if (hint.RoleFilter.Any())
                    {
                        if (!hint.CanShow(Player))
                            return;
                    }

                    m_Queue.Enqueue(hint);
                });

                m_Global.ForEach(hint =>
                {
                    if (hint.RoleFilter.Any())
                    {
                        if (!hint.CanShow(Player))
                            return;
                    }

                    m_Queue.Enqueue(hint);
                });
            }
            else
            {
                if (m_Queue.TryDequeue(out var hint))
                {
                    Display(hint);
                }
            }
        }
    }
}