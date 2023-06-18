using Hints;

using PlayerRoles;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Helpers.Hints
{
    public class Hint
    {
        private readonly HintWriter m_Writer;
        private readonly HintPriority m_Priority;
        private readonly Action<HintWriter> m_Update;
        private readonly float m_Duration;
        private readonly int m_MaxTimes;
        private readonly HintEffect[] m_Effects;
        private readonly RoleTypeId[] m_RoleFilter;

        private int m_ShownTimes = 0;

        public HintWriter Writer => m_Writer;
        public HintPriority Priority => m_Priority;
        public RoleTypeId[] RoleFilter => m_RoleFilter;
        public HintEffect[] Effects => m_Effects;

        public float Duration => m_Duration;
        public int MaxTimes => m_MaxTimes;

        public Hint(HintWriter writer, HintPriority priority, Action<HintWriter> update, float duration, int maxTimes, IEnumerable<HintEffect> effects, IEnumerable<RoleTypeId> roleFilter = null)
        {
            m_Writer = writer;
            m_Priority = priority;
            m_Update = update;
            m_Duration = duration;
            m_MaxTimes = maxTimes;
            m_Effects = effects?.ToArray();

            if (roleFilter is null)
                m_RoleFilter = Array.Empty<RoleTypeId>();
            else
                m_RoleFilter = roleFilter.ToArray();
        }

        public void Update()
        {
            m_Update?.Invoke(m_Writer);
        }
       
        public void Show(ReferenceHub hub)
        {
            m_ShownTimes++;

            var text = ToString();
            var hint = new TextHint(text, new HintParameter[] { new StringHintParameter(text) }, m_Effects, m_Duration);

            hub.hints.Show(hint);
        }

        public void Reset()
        {
            m_ShownTimes = 0;
        }

        public bool CanShow(ReferenceHub hub)
        {
            if (m_MaxTimes != -1 && m_ShownTimes >= m_MaxTimes)
                return false;

            if (m_RoleFilter.Any() && !m_RoleFilter.Contains(hub.GetRoleId()))
                return false;

            return true;
        }

        public override string ToString()
        {
            Update();
            return m_Writer.ToString();
        }
    }
}