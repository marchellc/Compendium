using Compendium.Updating;

using helpers;
using helpers.Configuration;
using helpers.Patching;
using helpers.Random;

using InventorySystem.Disarming;
using InventorySystem.Items;

using Mirror;

using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Events;

using System.Collections.Generic;
using System.Linq;

using Utils.Networking;

namespace Compendium.Escapes
{
    public static class EscapeHandler
    {
        [Config(Name = "Escape Roles", Description = "A list of roles and their escape role counterparts.")]
        public static Dictionary<RoleTypeId, Dictionary<int, RoleTypeId>> Escapes { get; set; } = new Dictionary<RoleTypeId, Dictionary<int, RoleTypeId>>()
        {
            [RoleTypeId.FacilityGuard] = new Dictionary<int, RoleTypeId>()
            {
                [70] = RoleTypeId.NtfSergeant,
                [30] = RoleTypeId.NtfPrivate
            }
        };

        [Config(Name = "Cuffed Escape Roles", Description = "A list of roles and their escape role counterparts.")]
        public static Dictionary<RoleTypeId, Dictionary<int, RoleTypeId>> CuffedEscapes { get; set; } = new Dictionary<RoleTypeId, Dictionary<int, RoleTypeId>>();

        [Config(Name = "Escape Radius", Description = "The radius of the escape area.")]
        public static float EscapeRadius { get; set; } = Escape.RadiusSqr;

        [Config(Name = "Cuff Radius", Description = "How close you need to be to a player to cuff him.")]
        public static float CuffRadius { get; set; } = 20f;

        [Config(Name = "Same Team Cuffs", Description = "Whether or not to allow players that are on the same team to be treated as cuffed when escaping.")]
        public static bool AllowSameTeamCuff { get; set; }

        [Config(Name = "Non Human Cuffs", Description = "Whether or not to allow players that are not human roles to be cuffed.")]
        public static bool AllowNonHumanCuff { get; set; }

        [Config(Name = "Additional Items", Description = "Items that can be used to disarm players.")]
        public static List<ItemType> AdditionalDisarmingItems { get; set; } = new List<ItemType>()
        {
            ItemType.KeycardFacilityManager
        };

        [Update(Delay = 500)]
        private static void OnUpdate()
        {
            if (Round.Duration.TotalSeconds < 15)
                return;

            Hub.Hubs.For((_, hub) =>
            {
                if (!hub.IsPlayer() || !hub.IsAlive())
                    return;

                if ((hub.Position() - Escape.WorldPos).sqrMagnitude > EscapeRadius)
                    return;

                var cuffer = hub.GetCuffer();

                if (cuffer != null 
                    && cuffer.GetFaction() == hub.GetFaction() 
                    && !AllowSameTeamCuff)
                    return;

                if (cuffer != null)
                {
                    if (CuffedEscapes.TryGetValue(hub.GetRoleId(), out var dict))
                    {
                        var chosen = WeightedRandomGeneration.Default.PickObject(x => x.Key, dict.ToArray());

                        if (chosen.Value is RoleTypeId.None)
                            return;

                        hub.roleManager.ServerSetRole(chosen.Value, RoleChangeReason.Escaped, RoleSpawnFlags.All);
                    }
                }
                else
                {
                    if (Escapes.TryGetValue(hub.RoleId(), out var dict))
                    {
                        var chosen = WeightedRandomGeneration.Default.PickObject(x => x.Key, dict.ToArray());

                        if (chosen.Value is RoleTypeId.None)
                            return;

                        hub.roleManager.ServerSetRole(chosen.Value, RoleChangeReason.Escaped, RoleSpawnFlags.All);
                    }
                }
            });
        }

        private static bool CanDisarm(ReferenceHub disarmer, ReferenceHub target)
        {
            if (!AllowSameTeamCuff && (disarmer.GetFaction() == target.GetFaction()))
                return false;

            if (!AllowNonHumanCuff && (!disarmer.IsHuman() || !target.IsHuman()))
                return false;

            if (target.interCoordinator.AnyBlocker(BlockedInteraction.BeDisarmed))
                return false;

            if (disarmer.inventory.CurInstance != null)
            {
                if (AdditionalDisarmingItems.Contains(disarmer.inventory.CurInstance.ItemTypeId))
                    return true;

                if (disarmer.inventory.CurInstance is IDisarmingItem disarmingItem)
                    return disarmingItem.AllowDisarming;
            }

            return false;
        }

        [Patch(typeof(DisarmingHandlers), nameof(DisarmingHandlers.ServerProcessDisarmMessage), PatchType.Prefix)]
        private static bool CuffPatch(NetworkConnection conn, DisarmMessage msg)
        {
            try
            {
                if (!DisarmingHandlers.ServerCheckCooldown(conn))
                    return false;

                if (!ReferenceHub.TryGetHub(conn.identity.gameObject, out var hub))
                    return false;

                if (!msg.PlayerIsNull)
                {
                    if ((msg.PlayerToDisarm.transform.position - hub.transform.position).sqrMagnitude >= CuffRadius)
                        return false;

                    if (msg.PlayerToDisarm.inventory.CurInstance != null
                        && msg.PlayerToDisarm.inventory.CurInstance.TierFlags != ItemTierFlags.Common)
                        return false;
                }

                var isDisarmed = !msg.PlayerIsNull && msg.PlayerToDisarm.inventory.IsDisarmed();
                var canDisarm = !msg.PlayerIsNull && CanDisarm(hub, msg.PlayerToDisarm);

                if (isDisarmed && !msg.Disarm)
                {
                    if (!hub.inventory.IsDisarmed())
                    {
                        var isScp = hub.IsSCP(true);
                        var ev = new PlayerRemoveHandcuffsEvent(hub, msg.PlayerToDisarm);

                        if (!EventManager.ExecuteEvent(ev))
                            return false;

                        if (isScp && ev.CanRemoveHandcuffsAsScp)
                            isScp = false;

                        if (isScp)
                            return false;

                        msg.PlayerToDisarm.inventory.SetDisarmedStatus(null);
                    }
                }
                else
                {
                    if (isDisarmed || !canDisarm || !msg.Disarm)
                        hub.connectionToClient.Send(DisarmingHandlers.NewDisarmedList);

                    if (msg.PlayerToDisarm.inventory.CurInstance is null
                        || msg.PlayerToDisarm.inventory.CurInstance.CanHolster())
                    {
                        if (!EventManager.ExecuteEvent(new PlayerHandcuffEvent(hub, msg.PlayerToDisarm)))
                            return false;

                        Reflection.TryInvokeEvent(typeof(DisarmingHandlers), "OnPlayerDisarmed", null, hub, msg.PlayerToDisarm);

                        msg.PlayerToDisarm.inventory.SetDisarmedStatus(hub.inventory);
                    }
                }

                DisarmingHandlers.NewDisarmedList.SendToAuthenticated();
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}