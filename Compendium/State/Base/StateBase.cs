using Compendium.State.Interfaced;
using PlayerRoles;
using PlayerStatsSystem;

namespace Compendium.State.Base
{
    public class StateBase : IState
    {
        private bool m_IsActive;

        internal ReferenceHub m_Player;

        public virtual string Name { get; }
        public virtual bool IsActive { get => m_IsActive; }

        public virtual StateFlags Flags { get; }

        public ReferenceHub Player => m_Player;

        public void Disable() => SetActive(false);
        public void Enable() => SetActive(true);
        public void SetActive(bool active)
        {
            m_IsActive = active;
            OnActiveUpdated();
        }

        public virtual void HandlePlayerSpawn(RoleTypeId newRole) { }
        public virtual void HandlePlayerDeath(DamageHandlerBase damageHandler) { }
        public virtual void HandlePlayerDamage(DamageHandlerBase damageHandler) { }

        public virtual void OnLoaded() { }
        public virtual void OnUnloaded() { }
        public virtual void OnUpdate() { }
        public virtual void OnActiveUpdated() { }

        void IState.Load() => OnLoaded();
        void IState.Unload() => OnUnloaded();
        void IState.SetPlayer(ReferenceHub hub) => m_Player = hub;
        void IState.Update()
        {
            if (!m_IsActive) return;
            OnUpdate();
        }
    }
}
