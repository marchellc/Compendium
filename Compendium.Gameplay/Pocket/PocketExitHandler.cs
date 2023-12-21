using Compendium.Constants;
using Compendium.Messages;
using Compendium.Staff;
using Compendium.Events;
using Compendium.Attributes;
using Compendium.Enums;

using helpers.Configuration;
using helpers.Patching;
using helpers.Random;
using helpers.Extensions;
using helpers;

using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp106;

using Mirror;

using PluginAPI.Events;

using System.Collections.Generic;

using UnityEngine;

using CustomPlayerEffects;

using PlayerStatsSystem;

using MapGeneration;
using Compendium.Features;

namespace Compendium.Gameplay.Pocket
{
    public static class PocketExitHandler
    {
        private static Dictionary<ReferenceHub, int> _escapedTimes = new Dictionary<ReferenceHub, int>();
        private static int _totalEscapes = 0;

        [Config(Name = "Failed Hint", Description = "The hint to display if a player fails to escape.")]
        public static HintMessage EscapeFailedHint { get; set; } = HintMessage.Create($"<b><color={Colors.LightGreenValue}><color={Colors.RedValue}>Nepovedlo</color> se ti utéct .. možná příště.</color></b>", 5);

        [Config(Name = "Escaped Hint", Description = "The hint to display if a player succesfully escapes.")]
        public static HintMessage EscapeSuccessHint { get; set; } = HintMessage.Create($"<b><color={Colors.LightGreenValue}><color={Colors.GreenValue}>Povedlo</color> se ti utéct! Dobrá práce.</color></b>", 5);

        [Config(Name = "Escaped Player Hint", Description = "The hint to display to all SCP-106 players when a player escapes.")]
        public static HintMessage EscapedScpHint { get; set; } = HintMessage.Create($"<b><color={Colors.GreenValue}>Hráči <color={Colors.RedValue}>%player%</color> (<color={Colors.LightGreenValue}>%role%</color>)se povedlo utéct z dimenze! Nyní se nacházejí v %zone%.</color></b>", 10);

        [Config(Name = "Exit Count", Description = "The amount of exits that are always correct.")]
        public static int AlwaysExitCount { get; set; } = 1;

        [Config(Name = "Escape Window", Description = "The amount of milliseconds to keep a count of player's escapes for. Chance of escape decreases based on this.")]
        public static int EscapeTimeWindow { get; set; } = 60000;

        [Config(Name = "Regenerate Count", Description = "The amount of escapes requiRedValue for the pocket dimension to regenerate. Set to zero to disable.")]
        public static int RegenerateAfterEscapes { get; set; } = 0;

        [Config(Name = "Escape Chances", Description = "A list of chances of escape.")]
        public static Dictionary<string, int> EscapeChances { get; set; } = new Dictionary<string, int>()
        {
            ["*"] = 20
        };

        [Patch(typeof(PocketDimensionGenerator), nameof(PocketDimensionGenerator.GenerateRandom), PatchType.Prefix)]
        private static bool GeneratePatch(PocketDimensionGenerator __instance)
        {
            var array = PocketDimensionGenerator.PrepTeleports();

            array.ForEach(t => t.SetType(PocketDimensionTeleport.PDTeleportType.Killer));

            if (AlwaysExitCount > 0)
            {
                for (int i = 0; i < AlwaysExitCount; i++)
                {
                    var randomIndex = RandomGeneration.Default.GetRandom(0, array.Length - 1);

                    while (array[randomIndex]._type is PocketDimensionTeleport.PDTeleportType.Exit)
                        randomIndex = RandomGeneration.Default.GetRandom(0, array.Length - 1);

                    array[randomIndex].SetType(PocketDimensionTeleport.PDTeleportType.Exit);
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i]._type != PocketDimensionTeleport.PDTeleportType.Exit)
                    array[i].SetType(PocketDimensionTeleport.PDTeleportType.Killer);
            }

            FLog.Info($"Pocket Dimension generated!");

            for (int i = 0; i < array.Length; i++)
                FLog.Info($"[{i}]: {array[i]._type}");

            return false;
        }

        [Patch(typeof(PocketDimensionTeleport), nameof(PocketDimensionTeleport.OnTriggerEnter))]
        private static bool ExitPatch(PocketDimensionTeleport __instance, Collider other)
        {
            var identity = other.GetComponent<NetworkIdentity>();

            if (identity is null)
                return false;

            if (!ReferenceHub.TryGetHubNetID(identity.netId, out var hub))
                return false;

            if (hub.roleManager.CurrentRole.ActiveTime < 1f)
                return false;

            if (!(hub.Role() is IFpcRole fpcRole))
                return false;

            if (__instance._type == PocketDimensionTeleport.PDTeleportType.Exit || hub.characterClassManager.GodMode)
            {
                Exit(hub, fpcRole);
                return false;
            }
            else
            {
                var chance = EscapeChances["*"];

                if (StaffHandler.Members.TryGetValue(hub.UserId(), out var groups))
                {
                    foreach (var group in groups)
                    {
                        if (EscapeChances.TryGetValue(group, out chance))
                            break;
                    }
                }

                if (_escapedTimes.ContainsKey(hub))
                    chance -= _escapedTimes[hub] * 10;

                if (chance < 0)
                    chance = 0;

                var canEscape = chance > 0 && WeightedRandomGeneration.Default.GetBool(chance);

                if (!canEscape)
                    FailExit(hub);
                else
                    Exit(hub, fpcRole);
            }

            return false;
        }

        private static void FailExit(ReferenceHub hub)
        {
            if (!EventManager.ExecuteEvent(new PlayerExitPocketDimensionEvent(hub, false)))
                return;

            hub.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.PocketDecay));

            if (EscapeFailedHint != null && EscapeFailedHint.IsValid)
                EscapeFailedHint.Send(hub);

            _escapedTimes.Remove(hub);
        }

        private static void Exit(ReferenceHub hub, IFpcRole fpcRole)
        {
            if (!EventManager.ExecuteEvent(new PlayerExitPocketDimensionEvent(hub, true)))
                return;

            fpcRole.FpcModule.ServerOverridePosition(Scp106PocketExitFinder.GetBestExitPosition(fpcRole), Vector3.zero);

            hub.playerEffectsController.EnableEffect<Disabled>(10f, true);
            hub.playerEffectsController.EnableEffect<Traumatized>();

            hub.playerEffectsController.DisableEffect<PocketCorroding>();
            hub.playerEffectsController.DisableEffect<Corroding>();

            Reflection.TryInvokeEvent(typeof(PocketDimensionTeleport), "OnPlayerEscapePocketDimension", hub);

            if (EscapeSuccessHint != null && EscapeSuccessHint.IsValid)
                EscapeSuccessHint.Send(hub);

            if (EscapedScpHint != null && EscapedScpHint.IsValid)
                Hub.ForEach(h =>
                {
                    h.Hint(EscapedScpHint.Value
                        .Replace("%player%", hub.Nick())
                        .Replace("%role%", $"<color={hub.GetRoleColorHexPrefixed()}>{hub.RoleId().ToString().SpaceByPascalCase()}</color>")
                        .Replace("%zone%", hub.Zone().ToString().SpaceByPascalCase())
                        .Replace("%room%", hub.RoomId().ToString().SpaceByPascalCase()), (float)EscapedScpHint.Duration);
                }, PlayerRoles.RoleTypeId.Scp106);

            _totalEscapes++;

            if (RegenerateAfterEscapes > 0 && _totalEscapes >= RegenerateAfterEscapes)
            {
                ImageGenerator.pocketDimensionGenerator?.GenerateRandom();
                _totalEscapes = 0;
            }

            if (_escapedTimes.ContainsKey(hub))
                _escapedTimes[hub]++;
            else
            {
                _escapedTimes.Add(hub, 1);
                Calls.Delay(EscapeTimeWindow, () => _escapedTimes.Remove(hub));
            }
        }

        [Event]
        private static void OnPlayerLeft(PlayerLeftEvent ev)
            => _escapedTimes.Remove(ev.Player.ReferenceHub);

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRoundRestart()
            => _escapedTimes.Clear();
    }
}