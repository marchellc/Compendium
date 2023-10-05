using System.Collections.Generic;

namespace Compendium.Custom.Scp914
{
    public static class Scp914Defaults
    {
        public static Dictionary<ItemType, Dictionary<string, Dictionary<ItemType, int>>> RoughDefaults { get; } = new Dictionary<ItemType, Dictionary<string, Dictionary<ItemType, int>>>
        {
            [ItemType.KeycardJanitor] = new Dictionary<string, Dictionary<ItemType, int>>() { ["*"] = new Dictionary<ItemType, int>() { [ItemType.None] = 100 } },
            [ItemType.KeycardScientist] = new Dictionary<string, Dictionary<ItemType, int>>() { ["*"] = new Dictionary<ItemType, int>() { [ItemType.None] = 50, [ItemType.KeycardJanitor] = 50 } },
            [ItemType.KeycardResearchCoordinator] = new Dictionary<string, Dictionary<ItemType, int>>() { ["*"] = new Dictionary<ItemType, int>() { [ItemType.KeycardJanitor] = 50, [ItemType.KeycardScientist] = 50 } },
            [ItemType.KeycardGuard] = new Dictionary<string, Dictionary<ItemType, int>>() { ["*"] = new Dictionary<ItemType, int>() { [ItemType.KeycardJanitor] = 50, [ItemType.KeycardScientist] = 50 } },
            [ItemType.KeycardMTFOperative] = new Dictionary<string, Dictionary<ItemType, int>>() { ["*"] = new Dictionary<ItemType, int>() { [ItemType.None] = 50, [ItemType.KeycardGuard] = 50 } },
            [ItemType.KeycardMTFCaptain] = new Dictionary<string, Dictionary<ItemType, int>>() { ["*"] = new Dictionary<ItemType, int>() { [ItemType.KeycardMTFOperative] = 100 } },
            [ItemType.KeycardFacilityManager] = new Dictionary<string, Dictionary<ItemType, int>>() { ["*"] = new Dictionary<ItemType, int>() { [ItemType.KeycardZoneManager] = 100 } },
        };
    }
}