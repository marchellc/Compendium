using helpers;
using helpers.Configuration.Ini;
using helpers.Patching;
using helpers.Random;

using PlayerRoles;

using System.Collections.Generic;
using System.Linq;

namespace Compendium.BetterEscapes
{
    public static class BetterEscapesLogic
    {
        [IniConfig(Name = "Allow Same Team", Description = "Whether or not to allow team members to disarm other team members.")]
        public static bool AllowTeamDisarming { get; set; } = true;

        [IniConfig(Name = "Allow Scp Disarming", Description = "Whether or not to allow SCPs to disarm other players.")]
        public static bool AllowScpDisarming { get; set; }

        [IniConfig(Name = "Require Item Disarming", Description = "Whether or not to require an item to disarm other players.")]
        public static bool RequireItemDisarming { get; set; } = true;

        [IniConfig(Name = "Override Item Disarming", Description = "Whether or not to override base-game's logic when it comes to determining which items can be used to disarm other players.")]
        public static bool OverrideItemDisarming { get; set; }

        [IniConfig(Name = "Disarming Items", Description = "A list of items that can be used to disarm other players.")]
        public static List<ItemType> DisarmingItems { get; set; } = new List<ItemType>()
        {
            ItemType.GunAK,
            ItemType.GunCrossvec,
            ItemType.GunRevolver,
            ItemType.GunCom45,
            ItemType.GunFSP9,
            ItemType.GunE11SR,
            ItemType.GunCOM15,
            ItemType.GunCOM18,
            ItemType.GunLogicer,
            ItemType.GunShotgun
        };

        [IniConfig(Name = "Normal Escape Matrix", Description = "A list of conversions for un-disarmed escapees.")]
        public static Dictionary<RoleTypeId, Dictionary<int, RoleTypeId>> NormalConversions { get; set; } = new Dictionary<RoleTypeId, Dictionary<int, RoleTypeId>>()
        {
            [RoleTypeId.FacilityGuard] = new Dictionary<int, RoleTypeId>()
            {
                [50] = RoleTypeId.NtfPrivate,
                [50] = RoleTypeId.NtfSergeant
            }
        };

        [IniConfig(Name = "Cuffed Escape Matrix", Description = "A list of conversions for disarmed escapees.")]
        public static Dictionary<RoleTypeId, Dictionary<int, RoleTypeId>> CuffedConversions { get; set; } = new Dictionary<RoleTypeId, Dictionary<int, RoleTypeId>>()
        {
            [RoleTypeId.FacilityGuard] = new Dictionary<int, RoleTypeId>()
            {
                [25] = RoleTypeId.ChaosConscript,
                [25] = RoleTypeId.ChaosMarauder,
                [25] = RoleTypeId.ChaosRepressor,
                [25] = RoleTypeId.ChaosRifleman
            },

            [RoleTypeId.NtfCaptain] = new Dictionary<int, RoleTypeId>()
            {
                [25] = RoleTypeId.ChaosConscript,
                [25] = RoleTypeId.ChaosMarauder,
                [25] = RoleTypeId.ChaosRepressor,
                [25] = RoleTypeId.ChaosRifleman
            },

            [RoleTypeId.NtfPrivate] = new Dictionary<int, RoleTypeId>()
            {
                [25] = RoleTypeId.ChaosConscript,
                [25] = RoleTypeId.ChaosMarauder,
                [25] = RoleTypeId.ChaosRepressor,
                [25] = RoleTypeId.ChaosRifleman
            },

            [RoleTypeId.NtfSergeant] = new Dictionary<int, RoleTypeId>()
            {
                [25] = RoleTypeId.ChaosConscript,
                [25] = RoleTypeId.ChaosMarauder,
                [25] = RoleTypeId.ChaosRepressor,
                [25] = RoleTypeId.ChaosRifleman
            },

            [RoleTypeId.NtfSpecialist] = new Dictionary<int, RoleTypeId>()
            {
                [25] = RoleTypeId.ChaosConscript,
                [25] = RoleTypeId.ChaosMarauder,
                [25] = RoleTypeId.ChaosRepressor,
                [25] = RoleTypeId.ChaosRifleman
            },
        };

        public static bool TryOverride(RoleTypeId escapingAs, RoleTypeId? disarmer, out RoleTypeId? overrideRole)
        {
            if (!AllowTeamDisarming)
            {
                if (disarmer.HasValue && disarmer.Value.GetFaction() == escapingAs.GetFaction())
                {
                    overrideRole = null;
                    return false;
                }
            }

            if (!AllowScpDisarming)
            {
                if (disarmer.HasValue && disarmer.Value.GetFaction() == Faction.SCP && escapingAs.GetFaction() == Faction.SCP)
                {
                    overrideRole = null;
                    return false;
                }
            }

            var conversions = disarmer.HasValue ? CuffedConversions : NormalConversions;

            if (!conversions.Any())
            {
                overrideRole = null;
                return false;
            }

            if (!conversions.TryGetValue(escapingAs, out var possibleRoles))
            {
                overrideRole = null;
                return false;
            }

            var picked = WeightedRandomGeneration.Default.PickObject(pair => pair.Key, possibleRoles.ToArray());

            if (picked.Value != RoleTypeId.None)
            {
                overrideRole = picked.Value;
                return true;
            }

            overrideRole = null;
            return false;
        }
    }
}