using Compendium.Colors;
using Compendium.Hints;
using Compendium.RemoteKeycard.Enums;

using System.Collections.Generic;

namespace Compendium.RemoteKeycard.Handlers.Doors
{
    public class DoorZombieConfig
    {
        public List<InteractableCategory> AllowedCategories { get; set; } = new List<InteractableCategory>()
        {
            InteractableCategory.EzDoor,
            InteractableCategory.HczDoor,
            InteractableCategory.LczDoor,
            InteractableCategory.SurfaceDoor
        };

        public int LastInteractionInterval { get; set; } = 10000;

        public bool Enabled { get; set; } = true;

        public float StartingHealth { get; set; } = 150f;
        public float DamagePerPlayer { get; set; } = 5f;

        public float RegenHealth { get; set; } = 5f;
        public float RegenSpeed { get; set; } = 1500f;

        public HintInfo InteractionHint { get; set; } = HintInfo.Get(
            $"<b><color={ColorValues.LightGreen}>Tyto dveře mají ještě <color={ColorValues.Red}>%hp%</color> HP!<b></color>\n" +
            $"<i><color={ColorValues.Green}>Tip:</color> sežeň si pár dalších zombie pro větší damage! (aktuální damage: %damage% HP / hit)</color></i>", 5f);
    }
}