using Compendium.Features;
using Compendium.Round;
using Compendium.Warns;
using Compendium.PlayerData;

using helpers.Attributes;
using helpers.Configuration;
using helpers;
using helpers.Extensions;
using helpers.Time;
using helpers.Pooling.Pools;

using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System;

using PluginAPI.Loader;

using PlayerRoles;

using MapGeneration.Distributors;

using Respawning;

using GameCore;

using UnityEngine;

using PlayerStatsSystem;

using Mirror;
using PlayerRoles.PlayableScps.Scp939;

namespace Compendium.Webhooks
{
    public static class WebhookHandler
    {
        private static readonly List<WebhookData> _webhooks = new List<WebhookData>();

        private static List<string> _plugins = new List<string>();
        private static Timer _infoTimer;
        private static bool _warnReg;
        private static string _ip;

        private static AlphaWarheadOutsitePanel _outsite;
        private static Scp079Generator[] _gens;

        public static IReadOnlyList<WebhookData> Webhooks => _webhooks;

        [Config(Name = "Webhooks", Description = "A list of webhooks.")]
        public static Dictionary<WebhookLog, List<WebhookConfigData>> WebhookList { get; set; } = new Dictionary<WebhookLog, List<WebhookConfigData>>()
        {
            [WebhookLog.Console] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.Server] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.Report] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.CheaterReport] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.BanPrivate] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.BanPublic] = new List<WebhookConfigData>() { new WebhookConfigData() }
        };

        [Config(Name = "Info Data", Description = "The data to include in Info webhooks.")]
        public static List<WebhookInfoData> InfoData { get; set; } = new List<WebhookInfoData>()
        {
            WebhookInfoData.RoundStatus,

            WebhookInfoData.RespawnTime,
            WebhookInfoData.RespawnTeam,

            WebhookInfoData.TotalPlayers,
            WebhookInfoData.TotalStaff,

            WebhookInfoData.PluginList,
            WebhookInfoData.AliveCi,
            WebhookInfoData.AliveNtf,
            WebhookInfoData.AliveScps,
            WebhookInfoData.AliveSpectators,

            WebhookInfoData.GeneratorStatus,
            WebhookInfoData.WarheadStatus,
            WebhookInfoData.ServerAddress
        };

        [Config(Name = "Event Log", Description = "A list of webhooks with their respective in-game events.")]
        public static Dictionary<string, List<WebhookEventLog>> EventLog { get; set; } = new Dictionary<string, List<WebhookEventLog>>()
        {
            ["empty"] = new List<WebhookEventLog>()
            {
                WebhookEventLog.GrenadeExploded,
                WebhookEventLog.GrenadeThrown,
                WebhookEventLog.PlayerCuff,
                WebhookEventLog.PlayerDamage,
                WebhookEventLog.PlayerSelfDamage,
                WebhookEventLog.PlayerSuicide,
                WebhookEventLog.PlayerAuth,
                WebhookEventLog.PlayerFriendlyDamage,
                WebhookEventLog.PlayerFriendlyKill,
                WebhookEventLog.PlayerJoined,
                WebhookEventLog.PlayerKill,
                WebhookEventLog.PlayerLeft,
                WebhookEventLog.PlayerUncuff,
                WebhookEventLog.RoundEnded,
                WebhookEventLog.RoundStarted,
                WebhookEventLog.RoundWaiting
            }
        };

        [Config(Name = "Private Bans Include IP", Description = "Whether or not to show user's IP address in a private ban log.")]
        public static bool PrivateBansIncludeIp { get; set; } = true;

        [Config(Name = "Reports Include IP", Description = "Whether or not to show user's IP address in reports.")]
        public static bool ReportsIncludeIp { get; set; } = true;

        [Config(Name = "Cheater Reports Include IP", Description = "Whether or not to show user's IP address in cheater reports.")]
        public static bool CheaterReportsIncludeIp { get; set; } = true;

        [Config(Name = "Send Time", Description = "The amount of milliseconds between each queue pull.")]
        public static int SendTime { get; set; } = 500;

        [Config(Name = "Info Time", Description = "The amount of milliseconds between each info pull.")]
        public static int InfoTime { get; set; } = 1000;

        [Config(Name = "Announce Reports", Description = "Whether or not to announce reports in-game.")]
        public static bool AnnounceReportsInGame { get; set; } = true;

        [Load]
        [Reload]
        public static void Reload()
        {
            if (!_warnReg)
            {
                WarnSystem.OnWarnIssued.Register(new Action<WarnData, PlayerDataRecord, PlayerDataRecord>(OnWarned));
                _warnReg = true;
            }

            _webhooks.Clear();

            foreach (var pair in WebhookList)
            {
                foreach (var hook in pair.Value)
                {
                    if (!string.IsNullOrWhiteSpace(hook.Url) && hook.Url != "empty")
                        _webhooks.Add(new WebhookData(pair.Key, hook.Url, hook.Content));
                    else
                        FLog.Warn($"Invalid webhook URL: {hook.Url} ({pair.Key})");
                }
            }

            foreach (var pair in EventLog)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Key != "empty")
                    _webhooks.Add(new WebhookEvent(pair.Key, pair.Value));
                else
                    FLog.Warn($"Invalid event webhook URL: {pair.Key}");
            }

            if (_webhooks.Any(w => w.Type is WebhookLog.Info))
            {
                if (_infoTimer is null)
                {
                    _infoTimer = new Timer(InfoTime);
                    _infoTimer.Elapsed += OnElapsed;
                    _infoTimer.Enabled = true;
                    _infoTimer.Start();

                    FLog.Info($"Started the info timer.");
                }
            }
            else
            {
                if (_infoTimer != null)
                {
                    _infoTimer.Elapsed -= OnElapsed;
                    _infoTimer.Enabled = false;
                    _infoTimer.Stop();
                    _infoTimer.Dispose();
                    _infoTimer = null;

                    FLog.Info($"Stopped the info timer.");
                }
            }           

            FLog.Info($"Loaded {_webhooks.Count} webhooks.");
        }

        public static string GetDamageName(DamageHandlerBase damageHandler)
        {
            if (damageHandler is WarheadDamageHandler)
                return "Alpha Warhead";

            if (damageHandler is Scp018DamageHandler)
                return "SCP-018";

            if (damageHandler is Scp049DamageHandler)
                return "SCP-049";

            if (damageHandler is Scp096DamageHandler)
                return "SCP-096";

            if (damageHandler is Scp939DamageHandler)
                return "SCP-939";

            if (damageHandler is DisruptorDamageHandler)
                return "3-X Particle Disruptor";

            if (damageHandler is JailbirdDamageHandler)
                return "Jailbird";

            if (damageHandler is MicroHidDamageHandler)
                return "Micro-HID";

            if (damageHandler is RecontainmentDamageHandler)
                return "Recontainment";

            if (damageHandler is ExplosionDamageHandler)
                return "Grenade";

            if (damageHandler is FirearmDamageHandler firearm)
                return firearm.WeaponType.ToString().SpaceByPascalCase();

            if (damageHandler is UniversalDamageHandler universal 
                && DeathTranslations.TranslationsById.TryGetValue(universal.TranslationId, out var translation))
                return translation.LogLabel;

            if (damageHandler is AttackerDamageHandler attacker)
            {
                if (attacker.Attacker.Hub != null)
                {
                    if (attacker.Attacker.Hub.IsSCP(true))
                        return attacker.Attacker.Role.ToString().SpaceByPascalCase();

                    if (attacker.Attacker.Hub.inventory.CurInstance != null)
                        return attacker.Attacker.Hub.inventory.CurInstance.ItemTypeId.ToString().SpaceByPascalCase();
                }

                return attacker.Attacker.Role.ToString().SpaceByPascalCase();
            }

            if (DamageHandlers.ConstructorsById.TryGetFirst(p => p.Value.Method.ReturnType == damageHandler.GetType(), out var constructor))
            {
                var hash = constructor.Value.GetType().FullName.GetStableHashCode();

                if (DamageHandlers.IdsByTypeHash.TryGetValue(hash, out var id) && DeathTranslations.TranslationsById.TryGetValue(id, out translation))
                    return translation.LogLabel;
            }

            return damageHandler.GetType().Name;
        }

        [RoundStateChanged(RoundState.InProgress)]
        private static void OnRoundStarted()
        {
            foreach (var w in _webhooks)
            {
                if (w is WebhookEvent ev)
                {
                    if (ev.AllowedEvents != null && ev.AllowedEvents.Contains(WebhookEventLog.RoundStarted))
                    {
                        ev.Event($"⚡ The round has started!");
                    }
                }
            }

            var script = GameObject.Find("OutsitePanelScript");

            if (script is null)
                return;

            _outsite = script.GetComponentInParent<AlphaWarheadOutsitePanel>();
            _gens = UnityEngine.Object.FindObjectsOfType<Scp079Generator>();
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnWaiting()
        {
            foreach (var w in _webhooks)
            {
                if (w is WebhookEvent ev)
                {
                    if (ev.AllowedEvents != null && ev.AllowedEvents.Contains(WebhookEventLog.RoundWaiting))
                    {
                        ev.Event($"⏳ Waiting for players ..");
                    }
                }
            }
        }

        [RoundStateChanged(RoundState.Ending)]
        private static void OnRoundEnded()
        {
            _gens = null;

            foreach (var w in _webhooks)
            {
                if (w is WebhookEvent ev)
                {
                    if (ev.AllowedEvents != null && ev.AllowedEvents.Contains(WebhookEventLog.RoundEnded))
                    {
                        ev.Event($"🛑 The round has ended!");
                    }
                }
            }
        }

        private static void OnWarned(WarnData warn, PlayerDataRecord issuer, PlayerDataRecord target)
        {
            if (!_webhooks.Any(w => w.Type == WebhookLog.Warn))
                return;

            var embed = new Discord.DiscordEmbed();

            embed.WithColor(System.Drawing.Color.Red);
            embed.WithTitle($"⚠️ {World.CurrentClearOrAlternativeServerName}");
            embed.WithField("🔗 Udělil", $"**{issuer.NameTracking.LastValue}** *({issuer.Id.Split('@')[0]})*", false);
            embed.WithField("🔗 Hráč", $"**{target.NameTracking.LastValue}** *({target.Id.Split('@')[0]} | {target.Ip})*", false);
            embed.WithField("❓ Důvod", warn.Reason, false);
            embed.WithFooter($"📝 {warn.Id} | 🕒 {warn.IssuedAt.ToString("F")}");

            foreach (var webhook in _webhooks)
            {
                if (webhook.Type != WebhookLog.Warn)
                    continue;

                webhook.Send(null, embed);
            }
        }

        private static void OnElapsed(object sender, ElapsedEventArgs e)
        {
            if (!InfoData.Any())
                return;

            var embed = new Discord.DiscordEmbed();

            embed.WithTitle($"ℹ️ {World.CurrentClearOrAlternativeServerName}");
            embed.WithFooter($"🕒 Poslední aktualizace: {helpers.Time.TimeUtils.LocalStringFull} (interval: {TimeSpan.FromMilliseconds(InfoTime).UserFriendlySpan()})");

            if (InfoData.Contains(WebhookInfoData.ServerAddress))
            {
                if (_ip is null)
                    _ip = $"**{ConfigFile.ServerConfig.GetString("server_ip", "auto")}:{ServerStatic.ServerPort}**";

                embed.WithField("🌐 IP Adresa", _ip, false);
            }

            if (InfoData.Contains(WebhookInfoData.RoundStatus))
                embed.WithField("🕒 Status kola", GetRoundStatus(), false);

            if (InfoData.Contains(WebhookInfoData.WarheadStatus))
                embed.WithField($"☣️ Status hlavice Alpha", GetWarheadStatus(), false);

            if (InfoData.Contains(WebhookInfoData.GeneratorStatus))
                embed.WithField($"⚡ Status generátorů", GetGeneratorStatus(), false);

            if (InfoData.Contains(WebhookInfoData.RespawnTime))
                embed.WithField($"🕒 Čas do respawnu", GetRespawnStatus(), false);

            if (InfoData.Contains(WebhookInfoData.TotalPlayers))
                embed.WithField($"🧑🏻‍🤝‍🧑🏻 Počet hráčů ({Hub.Count})", GetPlayerList(), false);

            if (InfoData.Contains(WebhookInfoData.PluginList))
            {
                if (!_plugins.Any())
                {
                    AssemblyLoader.InstalledPlugins.ForEach(pl =>
                    {
                        _plugins.Add($"*[{(pl.PluginName == "Compendium API" || pl.PluginName == "BetterCommands" ? "CUSTOM" : "NW API")}]* **{pl.PluginName}**");
                    });

                    FeatureManager.LoadedFeatures.ForEach(f =>
                    {
                        if (!f.IsEnabled)
                            return;

                        _plugins.Add($"*[CUSTOM]* **{f.Name}**");
                    });

                    _plugins = _plugins.OrderBy(p => p).ToList();
                }

                embed.WithField($"📝 Seznam pluginů ({_plugins.Count})", GetPluginList(), false);
            }

            foreach (var webhook in _webhooks)
            {
                if (webhook.Type != WebhookLog.Info)
                    continue;

                webhook.Send(null, embed);
            }
        }

        private static string GetPluginList()
        {
            var sb = StringBuilderPool.Pool.Get();

            _plugins.For((i, p) =>
            {
                sb.AppendLine($"- {p}");
            });

            return StringBuilderPool.Pool.PushReturn(sb);
        }

        private static string GetPlayerList()
        {
            var sb = StringBuilderPool.Pool.Get();

            if (InfoData.Contains(WebhookInfoData.AliveCi))
                sb.AppendLine($"- 🟢 **Chaos Insurgency**: {Hub.Hubs.Count(h => h.GetTeam() is Team.ChaosInsurgency)}");

            if (InfoData.Contains(WebhookInfoData.AliveNtf))
                sb.AppendLine($"- 🔵 **Nine-Tailed Fox**: {Hub.Hubs.Count(h => h.GetTeam() is Team.FoundationForces && h.RoleId() != RoleTypeId.FacilityGuard)}");

            if (InfoData.Contains(WebhookInfoData.AliveScps))
                sb.AppendLine($"- 🔴 **SCP**: {Hub.Hubs.Count(h => h.GetTeam() is Team.SCPs)}");

            if (InfoData.Contains(WebhookInfoData.AliveSpectators))
                sb.AppendLine($"- 💀 **Diváci**: {Hub.Hubs.Count(h => h.GetRoleId() is RoleTypeId.Spectator || h.GetRoleId() is RoleTypeId.Overwatch)}");

            if (InfoData.Contains(WebhookInfoData.TotalStaff))
                sb.AppendLine($"- 🧰 **Administrátoři**: {Hub.Hubs.Count(h => h.serverRoles.RemoteAdmin)}");

            return StringBuilderPool.Pool.PushReturn(sb);
        }

        private static string GetRespawnStatus()
        {
            if (RespawnManager.Singleton is null || !RoundHelper.IsStarted)
                return "Nelze zjistit!";

            if (InfoData.Contains(WebhookInfoData.RespawnTeam))
            {
                if (RespawnManager.Singleton._curSequence is RespawnManager.RespawnSequencePhase.SelectingTeam
                    && RespawnManager.Singleton.NextKnownTeam != SpawnableTeamType.None)
                    return $"{(RespawnManager.Singleton.NextKnownTeam is SpawnableTeamType.NineTailedFox ? "👮" : "🚔")} Tým **{(RespawnManager.Singleton.NextKnownTeam is SpawnableTeamType.NineTailedFox ? "Nine-Tailed Fox" : "Chaos Insurgency")}** se spawne za **{TimeSpan.FromSeconds(RespawnManager.Singleton.TimeTillRespawn).UserFriendlySpan()}**";

                if (RespawnManager.Singleton._curSequence is RespawnManager.RespawnSequencePhase.SpawningSelectedTeam)
                    return $"{(RespawnManager.Singleton.NextKnownTeam is SpawnableTeamType.NineTailedFox ? "👮" : "🚔")} Probíhá spawn týmu **{(RespawnManager.Singleton.NextKnownTeam is SpawnableTeamType.NineTailedFox ? "Nine-Tailed Fox" : "Chaos Insurgency")}**";
            }

            return $"⏳ Zbývá **{TimeSpan.FromSeconds(RespawnManager.Singleton.TimeTillRespawn).UserFriendlySpan()}** do respawnu.";
        }

        private static string GetGeneratorStatus()
        {
            if (_gens is null)
                return "Neznámý počet.";

            var activatedCount = _gens.Count(g => g.HasFlag(g.Network_flags, Scp079Generator.GeneratorFlags.Engaged));
            var totalCount = _gens.Length;

            return $"**{activatedCount} / {totalCount}**";
        }

        private static string GetWarheadStatus()
        {
            if (AlphaWarheadController.Detonated)
                return "💥 **Detonována**";

            if (AlphaWarheadController.InProgress)
                return $"⚠️ **Probíhá** *({Mathf.CeilToInt(AlphaWarheadController.TimeUntilDetonation)} sekund do detonace)*";

            if (AlphaWarheadOutsitePanel.nukeside != null
                && AlphaWarheadOutsitePanel.nukeside.Networkenabled
                && _outsite != null
                && _outsite.NetworkkeycardEntered)
                return "✅ **Připravena** k detonaci";

            if (AlphaWarheadOutsitePanel.nukeside != null
                && AlphaWarheadOutsitePanel.nukeside.Networkenabled)
                return "✅ **Páčka povolena, karta není vložena**";

            if (_outsite != null && _outsite.NetworkkeycardEntered)
                return "✅ **Karta vložena, páčka není povolena**";

            return "❎ **Není vložena karta ani povolena páčka**";
        }

        private static string GetRoundStatus()
        {
            switch (RoundHelper.State)
            {
                case RoundState.Ending:
                    return "⭕ **Konec**";

                case RoundState.InProgress:
                    return $"🟢 **Probíhá** *({TimeUtils.TicksToCompoundTime(RoundStart.RoundStartTimer.ElapsedTicks)})*";

                case RoundState.Restarting:
                    return "🟢 **Restartuje se** ..";

                case RoundState.WaitingForPlayers:
                    return "⏳ **Čeká se na hráče** ..";

                default:
                    return "Neznámý status kola!";
            }
        }
    }
}