using PlayerRoles;

using UnityEngine;

namespace Compendium.Snapshots
{
    public struct RoleSnapshot
    {
        public RoleTypeId Role;

        public Vector3 Position;
        public Quaternion Rotation;

        public InventorySnapshot Inventory;

        public string UnitName;

        public byte UnitId;

        public float Health;
        public float MaxHealth;
        public float Stamina;
        public float HumeShield;
        public float Vigor;

        public RoleSnapshot(ReferenceHub hub)
        {
            Role = hub.RoleId();

            UnitName = hub.UnitName();
            UnitId = hub.UnitId();

            Health = hub.Health();
            MaxHealth = hub.MaxHealth();
            Stamina = hub.Stamina();
            HumeShield = hub.HumeShield();
            Vigor = hub.Vigor();

            Position = hub.Position();
            Rotation = hub.Rotation();

            Inventory = hub.SaveInventory();
        }

        public void Apply(ReferenceHub hub)
        {
            if (hub.RoleId() != Role)
                hub.RoleId(Role, RoleSpawnFlags.None);

            if (hub.Role() is HumanRole)
                hub.SetUnitId(UnitId);

            hub.MaxHealth(MaxHealth);
            hub.Health(Health);
            hub.Stamina(Stamina);
            hub.HumeShield(HumeShield);
            hub.Vigor(Vigor);
            hub.Position(Position, Rotation);

            Inventory.Restore(hub);
        }
    }
}