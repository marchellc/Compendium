using PlayerRoles;
using PlayerStatsSystem;

namespace Compendium.State.Interfaced
{
    public interface IState
    {
        string Name { get; }

        bool IsActive { get; }

        StateFlags Flags { get; }

        ReferenceHub Player { get; }

        void HandlePlayerSpawn(RoleTypeId newRole);
        void HandlePlayerDeath(DamageHandlerBase damageHandler);
        void HandlePlayerDamage(DamageHandlerBase damageHandler);

        void Load();
        void Unload();
        void Update();

        void SetActive(bool active);
        void Disable();
        void Enable();
        void SetPlayer(ReferenceHub hub);
    }
}