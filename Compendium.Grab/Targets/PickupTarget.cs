using Compendium.Extensions;

using InventorySystem.Items.Pickups;

using UnityEngine;

namespace Compendium.Grab.Targets
{
    public class PickupTarget : IGrabTarget
    {
        private ItemPickupBase m_Pickup;
        private Rigidbody m_Rigidbody;
        private ReferenceHub m_Hub;

        private readonly bool m_OrigKinematic;
        private readonly bool m_OrigGravity;

        private readonly float m_OrigMass;

        private readonly RigidbodyConstraints m_OrigConstraints;

        private readonly Vector3 m_OrigPos;
        private readonly Quaternion m_OrigRot;

        public PickupTarget(ItemPickupBase pickupBase, ReferenceHub hub)
        {
            m_Hub = hub;
            m_Pickup = pickupBase;
            m_Pickup.gameObject.TryGet(out m_Rigidbody);

            m_OrigPos = m_Pickup.Position;
            m_OrigRot = m_Pickup.Rotation;

            if (m_Rigidbody != null)
            {
                m_OrigKinematic = m_Rigidbody.isKinematic;
                m_OrigMass = m_Rigidbody.mass;
                m_OrigGravity = m_Rigidbody.useGravity;
                m_OrigConstraints = m_Rigidbody.constraints;
                m_Rigidbody.isKinematic = true;
                m_Rigidbody.mass = 1;
                m_Rigidbody.useGravity = true;
                m_Rigidbody.constraints = RigidbodyConstraints.None;
            }
        }

        public Rigidbody Rigidbody => m_Rigidbody;

        public void Move()
        {
            if (m_Pickup is null)
                return;

            if (m_Hub is null)
                return;

            m_Pickup.Position = m_Hub.PlayerCameraReference.position + (m_Hub.PlayerCameraReference.forward * 2f);
            m_Pickup.Rotation = m_Hub.transform.rotation;
        }

        public void Release()
        {
            if (Rigidbody != null)
            {
                Rigidbody.isKinematic = m_OrigKinematic;
                Rigidbody.mass = m_OrigMass;
                Rigidbody.useGravity = m_OrigGravity;
                Rigidbody.constraints = m_OrigConstraints;
            }

            m_Pickup.Position = m_OrigPos;
            m_Pickup.Rotation = m_OrigRot;

            m_Pickup = null;
            m_Hub = null;
        }
    }
}