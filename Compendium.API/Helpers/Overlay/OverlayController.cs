using Compendium.State.Base;
using Compendium.State.Interfaced;

using helpers.Extensions;

using Hints;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Compendium.Helpers.Overlay
{
    public class OverlayController : CustomUpdateTimeStateBase, IRequiredState
    {
        public static readonly TextHint EmptyHint = new TextHint("", new HintParameter[] { new StringHintParameter("") } );

        private readonly List<OverlayPart> m_Overlay = new List<OverlayPart>();
        private readonly ConcurrentQueue<OverlayPart> m_MessageQueue = new ConcurrentQueue<OverlayPart>();

        private OverlayPart m_PriorMsg;

        private string m_OverlayStr = "";
        private string m_ActiveMsg;

        private DateTime m_MsgEnd;

        public IReadOnlyList<OverlayPart> Parts => m_Overlay;

        public override float UpdateInterval => 100f;

        public void Message(object message, float duration, bool hasPriority = false) => AddPart(new OverlayPart(message, duration, hasPriority));

        public void AddPart(OverlayPart overlayPart)
        {
            if (overlayPart.ElementType is OverlayElementType.Message)
            {
                if (overlayPart.IsPriority)
                {
                    m_PriorMsg = overlayPart;
                    return;
                }
                else
                {
                    m_MessageQueue.Enqueue(overlayPart);
                    return;
                }    
            }
            else
            {
                m_Overlay.Add(overlayPart);
            }
        }

        private void RebuildOverlay()
            => OverlayHelper.RebuildOverlay(ref m_OverlayStr, m_Overlay);

        public override void OnUpdate()
        {
            if (m_ActiveMsg != null && DateTime.Now >= m_MsgEnd)
            {
                m_ActiveMsg = null;
                m_MsgEnd = default;
            }

            if (m_PriorMsg is null)
            {
                if (m_ActiveMsg is null && m_MessageQueue.TryDequeue(out var message))
                {
                    m_ActiveMsg = message.Data();
                    m_MsgEnd = DateTime.Now.AddMilliseconds(message.Duration.Value);
                }
            }
            else
            {
                m_ActiveMsg = m_PriorMsg.Data();
                m_MsgEnd = DateTime.Now.AddMilliseconds(m_PriorMsg.Duration.Value);
                m_PriorMsg = null;
            }

            if (string.IsNullOrWhiteSpace(m_OverlayStr))
                RebuildOverlay();

            var overlay = "";

            if (m_ActiveMsg != null)
            {
                overlay = m_OverlayStr.Replace("$msg", m_ActiveMsg);
            }
            else
            {
                overlay = m_OverlayStr.Remove("$msg");
            }

            if (string.IsNullOrWhiteSpace(overlay))
                return;

            Player.hints.Show(EmptyHint);
            Player.hints.Show(new TextHint(overlay, new HintParameter[] { new StringHintParameter(overlay) }, null, 0.2f));
        }
    }
}