using helpers.Configuration.Ini;
using helpers.Patching;
using helpers.Random;

using InventorySystem.Items.Usables.Scp330;

using System.Collections.Generic;
using System.Linq;

namespace Compendium.Gameplay.Candies
{
    public static class CandyHandler
    {
        [IniConfig(Name = "Candy Chances", Description = "A list of candies and their chances to be picked.")]
        public static Dictionary<CandyKindID, int> Chances { get; set; } = new Dictionary<CandyKindID, int>()
        {
            [CandyKindID.Blue] = 16,
            [CandyKindID.Red] = 16,
            [CandyKindID.Yellow] = 16,
            [CandyKindID.Green] = 16,
            [CandyKindID.Pink] = 4,
            [CandyKindID.Rainbow] = 16,
            [CandyKindID.Purple] = 16
        };

        [Patch(typeof(Scp330Candies), nameof(Scp330Candies.GetRandom), PatchType.Prefix, "Candy Patch")]
        private static bool Patch(ref CandyKindID __result)
        {
            __result = WeightedRandomGeneration.Default.PickObject(pair => pair.Value, Chances.ToArray()).Key;
            return false;
        }
    }
}