using helpers.Configuration;
using helpers.Patching;
using helpers.Random;

using InventorySystem.Items.Usables.Scp330;

using System.Collections.Generic;
using System.Linq;

namespace Compendium.Gameplay.Candies
{
    public static class CandyHandler
    {
        /*
        [Config(Name = "Candy Chances", Description = "A list of candies and their chances to be picked.")]
        public static Dictionary<CandyKindID, int> Chances { get; set; } = new Dictionary<CandyKindID, int>()
        {
            [CandyKindID.Blue] = ,
            [CandyKindID.Red] = ,
            [CandyKindID.Yellow] = ,
            [CandyKindID.Green] = ,
            [CandyKindID.Pink] = ,
            [CandyKindID.Rainbow] = ,
            [CandyKindID.Purple] = ,
            [CandyKindID.Gray] = ,
            [CandyKindID.White] = ,
            [CandyKindID.Black] = ,
            [CandyKindID.Brown] = ,
            [CandyKindID.Evil] = ,
            [CandyKindID.Orange] = 
        };

        [Patch(typeof(Scp330Candies), nameof(Scp330Candies.GetRandom), PatchType.Prefix, "Candy Patch")]
        private static bool Patch(ref CandyKindID __result)
        {
            __result = WeightedRandomGeneration.Default.PickObject(pair => pair.Value, Chances.ToArray()).Key;
            return false;
        }
        */
    }
}