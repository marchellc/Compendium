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

        public void Disable() => m_IsActive = false;
        public void Enable() => m_IsActive = true;
        public void SetActive(bool active) => m_IsActive = active;

        public virtual void HandlePlayerSpawn(RoleTypeId newRole) { }
        public virtual void HandlePlayerDeath(DamageHandlerBase damageHandler) { }
        public virtual void HandlePlayerDamage(DamageHandlerBase damageHandler) { }

        public virtual void OnLoaded() { }
        public virtual void OnUnloaded() { }
        public virtual void OnUpdate() { }

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
