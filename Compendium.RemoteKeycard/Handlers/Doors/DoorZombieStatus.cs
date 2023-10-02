using System;
using System.Collections.Generic;

namespace Compendium.RemoteKeycard.Handlers.Doors
{
    public class DoorZombieStatus
    {
        public float RemainingHealth { get; set; } 
        public float StartingHealth { get; set; }

        public float RegenHealth { get; set; }
        public float RegenSpeed { get; set; }

        public float DamagePerPlayer { get; set; }

        public bool ActiveInteraction { get; set; }
        public bool Broken { get; set; }

        public int DamageMultiplier => CurrentInteractions.Count;

        public float Damage => DamagePerPlayer * DamageMultiplier;

        public DateTime LastInteraction { get; set; }
        public DateTime LastRegen { get; set; }

        public List<ReferenceHub> CurrentInteractions { get; } = new List<ReferenceHub>();
    }
}