using Compendium.Helpers.Hints;
using Compendium.State;
using Compendium.State.Base;

using MapGeneration.Distributors;

using MEC;

using PlayerRoles;

using PluginAPI.Core;

using Respawning;

using System;
using System.Linq;

using UnityEngine;

namespace Compendium.Common.RespawnTimer
{
    public class RespawnTimerController : RequiredStateBase
    {
        private Hint m_Hint;

        public override string Name => "Respawn Timer";

        public RespawnTimerController()
        {
            m_Hint = new HintBuilder()
                .WithUpdate(UpdateHint)
                .WithPriority(HintPriority.Higher)
                .WithDuration(1f)
                .WithFadeInAndOut(1f)
                .Build();
        }

        public override void HandlePlayerSpawn(RoleTypeId newRole)
        {
            Timing.CallDelayed(0.5f, () =>
            {
                if (!Player.IsAlive())
                {
                    if (Player.GetRoleId() != RoleTypeId.Overwatch)
                    {
                        ShowHint();
                    }
                    else
                    {
                        HideHint();
                    }
                }
                else
                {
                    HideHint();
                }
            });
        }

        public void ShowHint()
        {
            if (Player.TryGetState<HintController>(out var hints))
            {
                hints.Forced = m_Hint;
            }
        }

        public void HideHint()
        {
            if (Player.TryGetState<HintController>(out var hints))
            {
                hints.Forced = null;
            }
        }

        private void UpdateHint(HintWriter writer)
        {
            writer.Clear();

            writer.EmitAlign(HintAlign.Center);
            writer.EmitVerticalOffset(1.0);
            writer.Emit($"{Round.Duration.ToString("HH:mm:ss")}");
            writer.EmitTagEnd(HintTag.VOffset);
            writer.EmitTagEnd(HintTag.Align);

            var generators = GameObject.FindObjectsOfType<Scp079Generator>();

            writer.EmitAlign(HintAlign.Left);
            writer.EmitVerticalOffset(-1.5);
            writer.Emit($"<b>Activated Generators:</b> <i>{generators.Count(gen => gen.HasFlag(gen.Network_flags, Scp079Generator.GeneratorFlags.Engaged))} / {generators.Length}<i>");
            writer.EmitTagEnd(HintTag.VOffset);
            writer.EmitTagEnd(HintTag.Align);

            writer.EmitAlign(HintAlign.Right);
            writer.EmitVerticalOffset(-1.5);
            writer.Emit($"<b>SCPs:</b> <i><color=#ff0000>{ReferenceHub.AllHubs.Count(scp => scp.IsSCP())}</color></i>");
            writer.EmitTagEnd(HintTag.VOffset);
            writer.EmitTagEnd(HintTag.Align);

            writer.EmitAlign(HintAlign.Left);
            writer.EmitVerticalOffset(-2.5);
            writer.Emit($"<b>Warhead Status:</b> <i>{GetWarheadStatus()}<i>");
            writer.EmitTagEnd(HintTag.VOffset);
            writer.EmitTagEnd(HintTag.Align);
        }

        private static string GetWarheadStatus()
        {
            if (AlphaWarheadController.Singleton is null)
                return "<color=#ff0000>Unknown</color>";

            if (AlphaWarheadController.Detonated)
                return "<color=#ffa883>Detonated</color>";

            if (AlphaWarheadController.InProgress)
                return "<color=#ff6B33>In Progress</color>";

            if (AlphaWarheadOutsitePanel.nukeside != null)
            {
                if (AlphaWarheadOutsitePanel.nukeside.Networkenabled)
                    return "<color=#a2ff33>Enabled</color>";
                else
                    return "<color=#ff0000>Disabled</color>";
            }
            else
                return "<color=#ff0000>Unknown</color>";
        }

        private static void ColorTeam(ref string team)
        {
            if (team is "Chaos Insurgency")
                team = $"<color=#a2ff33>{team}</color>";
            else
                team = $"<color=#33a2ff{team}</color>";
        }

        private static bool TryGetRespawningTeam(out string team, out string time)
        {
            if (RespawnManager.Singleton != null
                && RespawnManager.Singleton._curSequence != RespawnManager.RespawnSequencePhase.RespawnCooldown
                && RespawnManager.Singleton.NextKnownTeam != SpawnableTeamType.None)
            {
                team = RespawnManager.Singleton.NextKnownTeam is SpawnableTeamType.ChaosInsurgency ? "Chaos Insurgency" : "Nine-Tailed Fox";
                time = TimeSpan.FromSeconds(RespawnManager.Singleton.TimeTillRespawn).ToString("HH:mm:ss");
                return true;
            }

            team = null;
            time = null;
            return false;
        }
    }
}