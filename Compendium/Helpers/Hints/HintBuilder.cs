using Hints;
using PlayerRoles;

using System;
using System.Collections.Generic;

namespace Compendium.Helpers.Hints
{
    public class HintBuilder
    {
        private Action<HintWriter> m_Update;

        private List<RoleTypeId> m_RoleFilter = new List<RoleTypeId>();
        private List<HintEffect> m_Effects = new List<HintEffect>();

        private HintWriter m_Writer = new HintWriter();
        private HintPriority m_Priority;

        private float m_Duration;
        private int m_MaxTimes;

        public HintBuilder()
        {
            m_Priority = HintPriority.Low;
            m_Duration = 1f;
            m_MaxTimes = -1;
        }

        public HintBuilder WithUpdate(Action<HintWriter> update)
        {
            m_Update = update;
            return this;
        }

        public HintBuilder WithFadeIn(float duration, float iterations = 1f, float start = 0f)
        {
            m_Effects.Add(HintEffectPresets.FadeIn(duration, start, iterations));
            return this;
        }

        public HintBuilder WithFadeOut(float duration, float iterations = 1f, float start = 0f)
        {
            m_Effects.Add(HintEffectPresets.FadeOut(duration, start, iterations));
            return this;
        }

        public HintBuilder WithFadeInAndOut(float window, float duration = 1f, float start = 0f)
        {
            m_Effects.AddRange(HintEffectPresets.FadeInAndOut(window, duration, start));
            return this;
        }

        public HintBuilder WithPulseAlpha(float floor, float peak, float iterations = 1f, float offset = 0f)
        {
            m_Effects.Add(HintEffectPresets.PulseAlpha(floor, peak, iterations, offset));
            return this;
        }

        public HintBuilder WithTrailingPulseAlpha(float floor, float peak, float startTrail, float iterations = 1f, float start = 0f, int count = 1)
        {
            m_Effects.Add(HintEffectPresets.TrailingPulseAlpha(floor, peak, startTrail, iterations, start, count));
            return this;
        }

        public HintBuilder WithPriority(HintPriority hintPriority)
        {
            m_Priority = hintPriority;
            return this;
        }

        public HintBuilder WithDuration(float duration)
        {
            m_Duration = duration;
            return this;
        }

        public HintBuilder WithRoleFilter(params RoleTypeId[] roles)
        {
            m_RoleFilter.Clear();
            m_RoleFilter.AddRange(roles);

            return this;
        }

        public HintBuilder Write(Action<HintWriter> writer)
        {
            writer?.Invoke(m_Writer);
            return this;
        }

        public Hint Build() 
            => new Hint(m_Writer, m_Priority, m_Update, m_Duration, m_MaxTimes, m_Effects, m_RoleFilter);
    }
}