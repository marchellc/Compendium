using helpers.Configuration;
using helpers.Patching;

using Mirror;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.PlayableScps.Scp173;

using System.Collections.Generic;
using System;

using Utils.Networking;

using RelativePositioning;

using MapGeneration;

using Compendium.Enums;
using Compendium.Attributes;

using BetterCommands;

namespace Compendium.Gameplay.Tutorial
{
    public static class TutorialHandler
    {
        public static readonly HashSet<uint> Scp173Wh = new HashSet<uint>();
        public static readonly HashSet<uint> Scp096Wh = new HashSet<uint>();

        [Config(Name = "Can Tutorial Block SCP-173", Description = "Whether or not to allow players playing as Tutorial to block SCP-173's movement.")]
        public static bool CanTutorialBlockScp173 { get; set; }

        [Config(Name = "Can Tutorial Enrage SCP-096", Description = "Whether or not to allow players playing as Tutorial to enrage SCP-096 by looking.")]
        public static bool CanTutorialEnrageScp096 { get; set; }

        [Config(Name = "Can Tutorial Be Targeted By SCP-049", Description = "Whether or not to allow players playing as Tutorial to become SCP-049's targets.")]
        public static bool CanTutorialBeTargetedByScp049 { get; set; }

        [Config(Name = "Can Tutorial Be Pocket Drop", Description = "Whether or not to allow players playing as Tutorial to be selected by the Pocket Dimension for an item drop.")]
        public static bool CanTutorialBePocketDrop { get; set; }

        [Patch(typeof(Scp106PocketItemManager), nameof(Scp106PocketItemManager.GetRandomValidSpawnPosition))]
        private static bool PocketItemSpawnPositionPatch(ref RelativePosition __result)
        {
            var num = 0;

            foreach (var hub in Hub.Hubs)
            {
                if (!CanTutorialBePocketDrop && hub.RoleId() is RoleTypeId.Tutorial)
                    continue;

                if (hub.Role() is IFpcRole fpcRole)
                {
                    var pos = fpcRole.FpcModule.Position;

                    if (pos.y >= Scp106PocketItemManager.HeightLimit.x
                        && Scp106PocketItemManager.TryGetRoofPosition(pos, out var vector))
                    {
                        Scp106PocketItemManager.ValidPositionsNonAlloc[num] = vector;

                        if (++num > 64)
                            break;
                    }
                }
            }    

            if (num > 0)
            {
                __result = new RelativePosition(Scp106PocketItemManager.ValidPositionsNonAlloc[UnityEngine.Random.Range(0, num)]);
                return false;
            }

            foreach (var room in RoomIdentifier.AllRoomIdentifiers)
            {
                if ((room.Zone is FacilityZone.HeavyContainment || room.Zone is FacilityZone.Entrance)
                    && Scp106PocketItemManager.TryGetRoofPosition(room.transform.position, out var vector))
                {
                    Scp106PocketItemManager.ValidPositionsNonAlloc[num] = vector;

                    if (++num > 64)
                        break;
                }
            }

            if (num <= 0)
                throw new InvalidOperationException($"GetRandomValidSpawnPosition found no valid spawn positions.");

            __result = new RelativePosition(Scp106PocketItemManager.ValidPositionsNonAlloc[UnityEngine.Random.Range(0, num)]);
            return false;
        }

        [Patch(typeof(Scp173ObserversTracker), nameof(Scp173ObserversTracker.IsObservedBy), PatchType.Prefix)]
        private static bool Scp173TargetPatch(Scp173ObserversTracker __instance, ReferenceHub target, float widthMultiplier, ref bool __result)
        {
            if (!CanTutorialBlockScp173 
                && target.RoleId() is RoleTypeId.Tutorial
                && !Scp173Wh.Contains(target.netId))
            {
                __result = false;
                return false;
            }

            return true;
        }

        [Patch(typeof(Scp096TargetsTracker), nameof(Scp096TargetsTracker.IsObservedBy), PatchType.Prefix)]
        private static bool Scp096TargetPatch(Scp096TargetsTracker __instance, ReferenceHub target, ref bool __result)
        {
            if (!CanTutorialEnrageScp096 
                && target.RoleId() is RoleTypeId.Tutorial
                && !Scp096Wh.Contains(target.netId))
            {
                __result = false;
                return false;
            }

            return true;
        }

        [Patch(typeof(Scp049SenseAbility), nameof(Scp049SenseAbility.ServerProcessCmd), PatchType.Prefix)]
        private static bool Scp049TargetPatch(Scp049SenseAbility __instance, NetworkReader reader)
        {
            if (CanTutorialBeTargetedByScp049)
                return true;
            if (!__instance.Cooldown.IsReady
            || !__instance.Duration.IsReady)
                return false;

            __instance.HasTarget = false;
            __instance.Target = reader.ReadReferenceHub();

            if (__instance.Target != null && __instance.Target.RoleId() is RoleTypeId.Tutorial)
                __instance.Target = null;

            if (__instance.Target is null)
            {
                __instance.Cooldown.Trigger(2.5);
                __instance.ServerSendRpc(true);

                return false;
            }

            if (__instance.Target.roleManager.CurrentRole is HumanRole humanRole)
            {
                var radius = humanRole.FpcModule.CharController.radius;
                var pos = humanRole.CameraPosition;

                if (!VisionInformation.GetVisionInformation(
                    __instance.Owner, 
                    __instance.Owner.PlayerCameraReference, 
                    
                    pos, 
                    radius, 
                    
                    __instance._distanceThreshold, 
                    
                    true,
                    true, 
                    
                    0, 
                    
                    false).IsLooking)
                    return false;
            }

            __instance.Duration.Trigger(20);
            __instance.HasTarget = true;
            __instance.ServerSendRpc(true);

            return false;
        }

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRoundRestart()
        {
            Scp173Wh.Clear();
            Scp096Wh.Clear();
        }

        [BetterCommands.Command("tutorialwh", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Whitelists a player from custom tutorial blocks.")]
        private static string TutorialWhitelistCommand(ReferenceHub sender, ReferenceHub target)
        {
            if (Scp173Wh.Contains(target.netId) || Scp096Wh.Contains(target.netId))
            {
                Scp096Wh.Remove(target.netId);
                Scp173Wh.Remove(target.netId);

                return $"Disabled tutorial whitelist of {target.Nick()}";
            }
            else
            {
                Scp096Wh.Add(target.netId);
                Scp173Wh.Add(target.netId);

                return $"Enabled tutorial whitelist of {target.Nick()}";
            }
        }
    }
}