using Compendium.Attributes;
using Compendium.Constants;
using Compendium.Events;

using helpers;
using helpers.Extensions;
using helpers.Time;

using InventorySystem.Items.Usables;

using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;

using PlayerStatsSystem;

using PluginAPI.Core;
using PluginAPI.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Compendium.Processors
{
    public static class RoundStatProcessor
    {
        public static bool IsLocked;

        public static int TotalKills = 0;
        public static int TotalScpKills = 0;

        public static int TotalScpDamage = 0;
        public static int TotalDamage = 0;

        public static int TotalHealsUsed = 0;

        public static int TotalDeaths = 0;

        public static int TotalExplosiveGrenades = 0;
        public static int TotalFlashGrenades = 0;
        public static int TotalScpGrenades = 0;

        public static int TotalEscapes = 0;
        public static int TotalScp079Assists = 0;

        public static TimeSpan FastestEscape = TimeSpan.MinValue;
        public static TimeSpan FastestDeath = TimeSpan.MinValue;

        public static RoleTypeId FastestEscapeRole = RoleTypeId.None;

        public static ReferenceHub FastestDeathPlayer = null;
        public static ReferenceHub FastestEscapePlayer = null;

        public static readonly Dictionary<ReferenceHub, int> HumanKills = new Dictionary<ReferenceHub, int>();
        public static readonly Dictionary<ReferenceHub, int> ScpKills = new Dictionary<ReferenceHub, int>();
        public static readonly Dictionary<ReferenceHub, int> HumanDamage = new Dictionary<ReferenceHub, int>();
        public static readonly Dictionary<ReferenceHub, int> ScpDamage = new Dictionary<ReferenceHub, int>();
        public static readonly Dictionary<ReferenceHub, int> Deaths = new Dictionary<ReferenceHub, int>();

        public static readonly Dictionary<ReferenceHub, int> ExplosiveGrenades = new Dictionary<ReferenceHub, int>();
        public static readonly Dictionary<ReferenceHub, int> FlashGrenades = new Dictionary<ReferenceHub, int>();
        public static readonly Dictionary<ReferenceHub, int> ScpGrenades = new Dictionary<ReferenceHub, int>();

        public static readonly Dictionary<ReferenceHub, int> HealsUsed = new Dictionary<ReferenceHub, int>();

        [Event]
        public static void OnAssist(Scp079GainExperienceEvent ev)
        {
            if (IsLocked)
                return;

            if (ev.Player is null)
                return;

            if (ev.Reason != Scp079HudTranslation.ExpGainTerminationAssist)
                return;

            TotalScp079Assists++;
        }

        [Event]
        public static void OnDamage(PlayerDamageEvent ev)
        {
            if (IsLocked)
                return;

            if (ev.Player is null || ev.DamageHandler is null)
                return;

            if (ev.Player.IsSCP)
                TotalScpDamage += Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
            else
                TotalDamage += Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);

            if (ev.DamageHandler is AttackerDamageHandler attackerDamage
                && attackerDamage.Attacker.Hub != null
                && !attackerDamage.Attacker.Hub.IsServer())
            {
                if (ev.Player.IsSCP)
                {
                    if (!ScpDamage.ContainsKey(attackerDamage.Attacker.Hub))
                        ScpDamage[attackerDamage.Attacker.Hub] = Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
                    else
                        ScpDamage[attackerDamage.Attacker.Hub] += Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
                }
                else
                {
                    if (!HumanDamage.ContainsKey(attackerDamage.Attacker.Hub))
                        HumanDamage[attackerDamage.Attacker.Hub] = Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);
                    else
                        HumanDamage[attackerDamage.Attacker.Hub] += Mathf.CeilToInt((ev.DamageHandler as StandardDamageHandler).Damage);                    
                }
            }
        }

        [Event]
        public static void OnUsable(PlayerUsedItemEvent ev)
        {
            if (IsLocked)
                return;

            if (ev.Item is null)
                return;

            if (ev.Item is not Medkit)
                return;

            TotalHealsUsed++;

            if (ev.Player is null)
                return;

            if (!HealsUsed.ContainsKey(ev.Player.ReferenceHub))
                HealsUsed[ev.Player.ReferenceHub] = 1;
            else
                HealsUsed[ev.Player.ReferenceHub]++;
        }

        [Event]
        public static void OnEscaped(PlayerEscapeEvent ev)
        {
            if (IsLocked)
                return;

            TotalEscapes++;
        
            if (ev.Player != null)
            {
                if (FastestEscape <= TimeSpan.MinValue)
                {
                    FastestEscape = Round.Duration;
                    FastestEscapePlayer = ev.Player.ReferenceHub;
                    FastestEscapeRole = ev.Player.Role;
                }
            }
        }

        [Event]
        public static void OnGrenadeThrown(PlayerThrowProjectileEvent ev)
        {
            if (IsLocked)
                return;

            if (ev.Item is null)
                return;

            if (ev.Item.ItemTypeId is ItemType.SCP018)
            {
                TotalScpGrenades++;

                if (ev.Thrower != null)
                {
                    if (!ScpGrenades.ContainsKey(ev.Thrower.ReferenceHub))
                        ScpGrenades[ev.Thrower.ReferenceHub] = 1;
                    else
                        ScpGrenades[ev.Thrower.ReferenceHub]++;
                }
            }    
            else if (ev.Item.ItemTypeId is ItemType.GrenadeHE)
            {
                TotalExplosiveGrenades++;

                if (ev.Thrower != null)
                {
                    if (!ExplosiveGrenades.ContainsKey(ev.Thrower.ReferenceHub))
                        ExplosiveGrenades[ev.Thrower.ReferenceHub] = 1;
                    else
                        ExplosiveGrenades[ev.Thrower.ReferenceHub]++;
                }
            }
            else if (ev.Item.ItemTypeId is ItemType.GrenadeFlash)
            {
                TotalFlashGrenades++;

                if (ev.Thrower != null)
                {
                    if (!FlashGrenades.ContainsKey(ev.Thrower.ReferenceHub))
                        FlashGrenades[ev.Thrower.ReferenceHub] = 1;
                    else
                        FlashGrenades[ev.Thrower.ReferenceHub]++;
                }
            }
        }

        [Event]
        public static void OnDeath(PlayerDeathEvent ev)
        {
            if (IsLocked)
                return;

            TotalDeaths++;

            if (ev.Player is null || ev.DamageHandler is null)
                return;

            if (!Deaths.ContainsKey(ev.Player.ReferenceHub))
                Deaths[ev.Player.ReferenceHub] = 1;
            else
                Deaths[ev.Player.ReferenceHub]++;

            if (FastestDeath <= TimeSpan.MinValue)
            {
                FastestDeath = Round.Duration;
                FastestDeathPlayer = ev.Player.ReferenceHub;
            }

            if (ev.Player.IsSCP)
                TotalScpKills++;
            else
                TotalKills++;

            if (ev.DamageHandler is AttackerDamageHandler attackerDamage
                && attackerDamage.Attacker.Hub != null
                && !attackerDamage.Attacker.Hub.IsServer())
            {
                if (ev.Player.IsSCP)
                {
                    if (!ScpKills.ContainsKey(attackerDamage.Attacker.Hub))
                        ScpKills[attackerDamage.Attacker.Hub] = 1;
                    else
                        ScpKills[attackerDamage.Attacker.Hub]++;
                }
                else
                {
                    if (!HumanKills.ContainsKey(attackerDamage.Attacker.Hub))
                        HumanKills[attackerDamage.Attacker.Hub] = 1;
                    else
                        HumanKills[attackerDamage.Attacker.Hub]++;
                }
            }
        }

        [RoundStateChanged(Enums.RoundState.WaitingForPlayers)]
        public static void OnWaiting()
        {
            TotalKills = 0;
            TotalScpKills = 0;
            TotalScpDamage = 0;
            TotalHealsUsed = 0;
            TotalExplosiveGrenades = 0;
            TotalFlashGrenades = 0;
            TotalScpGrenades = 0;
            TotalEscapes = 0;
            TotalScp079Assists = 0;
            TotalDeaths = 0;

            FastestEscape = TimeSpan.MinValue;
            FastestDeath = TimeSpan.MinValue;

            FastestEscapeRole = RoleTypeId.None;

            FastestEscapePlayer = null;
            FastestDeathPlayer = null;

            HumanKills.Clear();
            ScpKills.Clear();
            HumanDamage.Clear();
            ScpDamage.Clear();
            ExplosiveGrenades.Clear();
            FlashGrenades.Clear();
            ScpGrenades.Clear();
            HealsUsed.Clear();
            Deaths.Clear();

            IsLocked = false;
        }

        [RoundStateChanged(Enums.RoundState.Ending)]
        public static void OnRoundEnd()
        {
            IsLocked = true;

            foreach (var hub in Hub.Hubs)
            {
                var sb = Pools.PoolStringBuilder();

                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();

                sb.AppendLine($"<size=18><align=left>");
                sb.AppendLine($"<b><color={Colors.RedValue}>[STATISTIKY - GLOBÁLNÍ]</color></b>");

                sb.AppendLine($"<b><color={Colors.LightGreenValue}>Počet zabití: <color={Colors.GreenValue}>{TotalKills}</color> / <color={Colors.GreenValue}>{TotalScpKills}</color> (<color={Colors.RedValue}>SCP</color>)</color></b>");
                sb.AppendLine($"<b><color={Colors.LightGreenValue}>Damage: <color={Colors.GreenValue}>{TotalDamage} HP</color> / <color={Colors.GreenValue}>{TotalScpDamage}</color> HP (<color={Colors.RedValue}>SCP</color>)</color></b>");
                sb.AppendLine($"<b><color={Colors.LightGreenValue}>Počet použitých medkitů: <color={Colors.GreenValue}>{TotalHealsUsed}</color></color></b>");
                sb.AppendLine($"<b><color={Colors.LightGreenValue}>Počet smrtí: <color={Colors.GreenValue}>{TotalDeaths}</color></color></b>");
                sb.AppendLine($"<b><color={Colors.LightGreenValue}>Počet granátů: <color={Colors.GreenValue}>{TotalExplosiveGrenades} HE / <color={Colors.GreenValue}>{TotalFlashGrenades}</color> FL / <color={Colors.GreenValue}>{TotalScpGrenades}</color> SCP</color></color></b>");
                sb.AppendLine($"<b><color={Colors.LightGreenValue}>Počet útěků: <color={Colors.GreenValue}>{TotalEscapes}</color></color></b>");
                sb.AppendLine($"<b><color={Colors.LightGreenValue}>Počet asistencí SCP-079: <color={Colors.GreenValue}>{TotalScp079Assists}</color></color></b>");

                if (FastestEscapeRole != RoleTypeId.None) 
                    sb.AppendLine($"<b><color={Colors.RedValue}>První útěk: <color={Colors.LightGreenValue}>{FastestEscapePlayer?.Nick() ?? "Neznámý hráč"}</color> za <color=#{FastestEscapeRole.GetRoleColorHex()}>{FastestEscapeRole.ToString().SpaceByPascalCase()}</color> (<color={Colors.RedValue}>{FastestEscape.UserFriendlySpan()}</color>)</color></b>");

                if (FastestDeathPlayer != null)
                    sb.AppendLine($"<b><color={Colors.RedValue}>První smrt: <color={Colors.LightGreenValue}>{FastestDeathPlayer?.Nick() ?? "Neznámý hráč"}</color> (<color={Colors.RedValue}>{FastestDeath.UserFriendlySpan()}</color>)</color></b>");

                var kills = HumanKills.OrderByDescending(p => p.Value).FirstOrDefault();

                if (kills.Key != null)
                    sb.AppendLine($"<b><color={Colors.RedValue}>Nejvíce zabití: <color={Colors.LightGreenValue}>{kills.Key.Nick()}</color> (<color={Colors.LightGreenValue}>{kills.Value}</color>)</color></b>");

                sb.AppendLine();
                sb.AppendLine($"<b><color={Colors.RedValue}>[STATISTIKY - PERSONÁLNÍ]</color></b>");

                sb.AppendLine($"<b><color={Colors.GreenValue}>Zabití: <color={Colors.LightGreenValue}>{(HumanKills.TryGetValue(hub, out var hKills) ? hKills : 0)}</color> / <color={Colors.LightGreenValue}>{(ScpKills.TryGetValue(hub, out var sKills) ? sKills : 0)}</color> (<color={Colors.RedValue}>SCP</color>)</color></b>");
                sb.AppendLine($"<b><color={Colors.GreenValue}>Smrtí: <color={Colors.LightGreenValue}>{(Deaths.TryGetValue(hub, out var d) ? d : 0)}</color></color></b>");
                sb.AppendLine($"<b><color={Colors.GreenValue}>Granátů: <color={Colors.LightGreenValue}>{(ExplosiveGrenades.TryGetValue(hub, out var hG) ? hG : 0)} HE / <color={Colors.LightGreenValue}>{(FlashGrenades.TryGetValue(hub, out var fH) ? fH : 0)}</color> FLASH / <color={Colors.LightGreenValue}>{(ScpGrenades.TryGetValue(hub, out var sH) ? sH : 0)}</color> SCP</color></color></b>");
                sb.AppendLine($"<b><color={Colors.GreenValue}>Medkitů: <color={Colors.LightGreenValue}>{(HealsUsed.TryGetValue(hub, out var hV) ? hV : 0)}</color></color></b>");
                sb.AppendLine($"<b><color={Colors.GreenValue}>Damage: <color={Colors.LightGreenValue}>{(HumanDamage.TryGetValue(hub, out var hDamage) ? hDamage : 0)} HP</color> / <color={Colors.LightGreenValue}>{(ScpDamage.TryGetValue(hub, out var sDamage) ? sDamage : 0)}</color> HP (<color={Colors.RedValue}>SCP</color>)</color></b>");

                sb.AppendLine($"</align></size>");

                hub.Hint(sb.ReturnStringBuilderValue(), 50f);
            }
        }
    }
}