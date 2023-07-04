using PlayerRoles.FirstPersonControl;

using UnityEngine;

namespace Compendium.Grab.Targets
{
    public class HubTarget : IGrabTarget
    {
        private ReferenceHub m_Hub;
        private ReferenceHub m_Target;

        private readonly Vector3 m_OrigPos;
        private readonly Quaternion m_OrigRot;

        public HubTarget(ReferenceHub target, ReferenceHub hub)
        {
            m_Target = target;
            m_Hub = hub;

            m_OrigPos = target.PlayerCameraReference.position;
            m_OrigRot = target.PlayerCameraReference.rotation;
        }

        public void Move()
        {
            if (m_Target is null)
                return;

            if (m_Hub is null)
                return;

            m_Target.TryOverridePosition(m_Hub.PlayerCameraReference.position + (m_Hub.PlayerCameraReference.forward * 2f), m_Hub.PlayerCameraReference.eulerAngles);
        }

        public void Release()
        {
            m_Target.TryOverridePosition(m_OrigPos, m_OrigRot.eulerAngles);

            m_Target = null;
            m_Hub = null;
        }
    }
}
