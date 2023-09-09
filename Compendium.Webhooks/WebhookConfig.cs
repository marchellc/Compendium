using Compendium.Features;
using Compendium.Round;
using Compendium.Events;

using helpers.Attributes;
using helpers.Configuration;
using helpers.Extensions;
using helpers.Time;
using helpers.Pooling.Pools;

using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System;

using PluginAPI.Events;
using PluginAPI.Loader;

using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;

using MapGeneration.Distributors;

using Respawning;

using GameCore;

using UnityEngine;
using Compendium.Warns;

namespace Compendium.Webhooks
{
    public static class WebhookConfig
    {
        private static readonly List<WebhookData> _webhooks = new List<WebhookData>();

        private static List<string> _plugins = new List<string>();
        private static Timer _infoTimer;
        private static bool _warnReg;
        private static string _ip;

        private static AlphaWarheadOutsitePanel _outsite;

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

        [Load]
        [Reload]
        public static void Reload()
        {
            if (!_warnReg)
            {
                WarnSystem.OnWarnIssued.Register(new Action<WarnData, ReferenceHub, ReferenceHub>(OnWarned));
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

        [Event]
        private static void OnRoundStarted(RoundStartEvent ev)
        {
            var script = GameObject.Find("OutsitePanelScript");

            if (script is null)
                return;

            _outsite = script.GetComponentInParent<AlphaWarheadOutsitePanel>();
        }

        private static void OnWarned(WarnData warn, ReferenceHub issuer, ReferenceHub target)
        {
            if (!_webhooks.Any(w => w.Type == WebhookLog.Warn))
                return;

            var embed = new Discord.DiscordEmbed();

            embed.WithTitle($"⚠️ {ServerConsole._serverName.RemoveHtmlTags()}");
            embed.WithField("Udělil", $"{issuer.Nick()} ({issuer.UserId()})", false);
            embed.WithField("Hráč", $"{target.Nick()} ({target.UserId()}", false);
            embed.WithField("Důvod", warn.Reason, false);
            embed.WithFooter($"{warn.Id} | {warn.IssuedAt.ToString("F")}");

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

            embed.WithTitle($"ℹ️ {ServerConsole._serverName.RemoveHtmlTags()}");
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
            var generators = Scp079InteractableBase.AllInstances.Where<Scp079Generator>();
            var activatedCount = generators.Count(g => g.HasFlag(g.Network_flags, Scp079Generator.GeneratorFlags.Engaged));
            var totalCount = generators.Count;

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