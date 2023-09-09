using Compendium.Events;
using Compendium.Features;

using helpers.Extensions;

using PluginAPI.Core;
using PluginAPI.Events;

using System;
using System.Timers;

namespace Compendium.Webhooks
{
    public class WebhookFeature : ConfigFeatureBase
    {
        public override string Name => "Webhooks";
        public override bool IsPatch => true;

        public override bool CanBeShared => false;

        [Event]
        private static void OnBanned(PlayerBannedEvent ev)
        {
            foreach (var webhook in WebhookConfig.Webhooks)
            {
                if (webhook.Type is WebhookLog.BanPrivate)
                {
                    var issuerField = new Discord.DiscordEmbedField();
                    var bannedField = new Discord.DiscordEmbedField();
                    var durationField = new Discord.DiscordEmbedField();
                    var reasonField = new Discord.DiscordEmbedField();
                    var footer = new Discord.DiscordEmbedFooter();
                    var author = new Discord.DiscordEmbedAuthor();
                    var embed = new Discord.DiscordEmbed();
                    var reason = string.IsNullOrWhiteSpace(ev.Reason) ? "No reason provided." : ev.Reason;

                    author.WithName($"{ServerConsole._serverName.RemoveHtmlTags()}");

                    reasonField.WithName($"❔ Reason");
                    reasonField.WithValue($"```{reason}```", false);

                    issuerField.WithName($"🔗 Issuer");
                    bannedField.WithName($"🔗 Player");

                    durationField.WithName($"🕒 Duration");
                    durationField.WithValue($"{TimeUtils.SecondsToCompoundTime(ev.Duration)}", false);

                    if (WebhookConfig.PrivateBansIncludeIp)
                    {
                        issuerField.WithValue(
                            $"**Username**: {ev.Issuer.Nickname}\n" +
                            $"**User ID**: {ev.Issuer.UserId}", false);

                        bannedField.WithValue(
                            $"**Username**: {ev.Player.Nickname}\n" +
                            $"**User ID**: {ev.Player.UserId}\n" +
                            $"**Player IP**: {(ev.Player is Player ply ? ply.ReferenceHub.Ip() : ev.Player.IpAddress)}", false);
                    }
                    else
                    {
                        issuerField.WithValue(
                            $"**Username**: {ev.Issuer.Nickname}\n" +
                            $"**User ID**: {ev.Issuer.UserId}", false);

                        bannedField.WithValue(
                            $"**Username**: {ev.Player.Nickname}\n" +
                            $"**User ID**: {ev.Player.UserId}\n", false);
                    }

                    footer.WithText(
                        $"🕒 Banned at: {DateTime.Now.ToLocalTime().ToString("G")}\n" +
                        $"🕒 Expires at: {(DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(ev.Duration)).ToString("G")}");

                    embed.WithAuthor(author);
                    embed.WithColor(System.Drawing.Color.Red);
                    embed.WithFields(issuerField, bannedField, durationField, reasonField);
                    embed.WithFooter(footer);
                    embed.WithTitle($"⚠️ Private Ban Log");

                    webhook.Send(null, embed);
                }
                else if (webhook.Type is WebhookLog.BanPublic)
                {
                    var issuerField = new Discord.DiscordEmbedField();
                    var bannedField = new Discord.DiscordEmbedField();
                    var reasonField = new Discord.DiscordEmbedField();
                    var durationField = new Discord.DiscordEmbedField();
                    var footer = new Discord.DiscordEmbedFooter();
                    var author = new Discord.DiscordEmbedAuthor();
                    var embed = new Discord.DiscordEmbed();
                    var reason = string.IsNullOrWhiteSpace(ev.Reason) ? "No reason provided." : ev.Reason;

                    author.WithName($"{ServerConsole._serverName.RemoveHtmlTags()}");

                    reasonField.WithName($"❔ Reason");
                    reasonField.WithValue($"```{reason}```", false);

                    issuerField.WithName($"🔗 Issuer");
                    bannedField.WithName($"🔗 Player");

                    durationField.WithName($"🕒 Duration");
                    durationField.WithValue($"{TimeUtils.SecondsToCompoundTime(ev.Duration)}", false);

                    issuerField.WithValue($"**{ev.Issuer.Nickname}**", false);
                    bannedField.WithValue($"**{ev.Player.Nickname}**", false);

                    footer.WithText(
                        $"🕒 Banned at: {DateTime.Now.ToLocalTime().ToString("G")}\n" +
                        $"🕒 Expires at: {(DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(ev.Duration)).ToString("G")}");

                    embed.WithAuthor(author);
                    embed.WithColor(System.Drawing.Color.Red);
                    embed.WithFields(issuerField, bannedField, durationField, reasonField);
                    embed.WithFooter(footer);
                    embed.WithTitle($"⚠️ Public Ban Log");

                    webhook.Send(null, embed);
                }
            }
        }

        [Event]
        private static void OnReport(PlayerReportEvent ev)
        {
            var reporterField = new Discord.DiscordEmbedField();
            var reportedField = new Discord.DiscordEmbedField();
            var reasonField = new Discord.DiscordEmbedField();
            var footer = new Discord.DiscordEmbedFooter();
            var author = new Discord.DiscordEmbedAuthor();
            var embed = new Discord.DiscordEmbed();

            author.WithName($"{ServerConsole._serverName.RemoveHtmlTags()}");

            reasonField.WithName($"❔ Reason");
            reasonField.WithValue($"```{ev.Reason}```", false);

            reporterField.WithName($"🔗 Reporting Player");
            reportedField.WithName($"🔗 Reported Player");

            if (WebhookConfig.ReportsIncludeIp)
            {
                reporterField.WithValue(
                    $"**Username**: {ev.Player.Nickname}\n" +
                    $"**User ID**: {ev.Player.UserId}\n" +
                    $"**Player IP**: {ev.Player.ReferenceHub.Ip()}\n" +
                    $"**Player ID**: {ev.Player.PlayerId}\n" +
                    $"**Player Role**: {ev.Player.Role.ToString().SpaceByPascalCase()}", false);

                reportedField.WithValue(
                    $"**Username**: {ev.Target.Nickname}\n" +
                    $"**User ID**: {ev.Target.UserId}\n" +
                    $"**Player IP**: {ev.Target.ReferenceHub.Ip()}\n" +
                    $"**Player ID**: {ev.Target.PlayerId}\n" +
                    $"**Player Role**: {ev.Target.Role.ToString().SpaceByPascalCase()}", false);
            }
            else
            {
                reporterField.WithValue(
                    $"**Username**: {ev.Player.Nickname}\n" +
                    $"**User ID**: {ev.Player.UserId}\n" +
                    $"**Player ID**: {ev.Player.PlayerId}\n" +
                    $"**Player Role**: {ev.Player.Role.ToString().SpaceByPascalCase()}", false);

                reportedField.WithValue(
                    $"**Username**: {ev.Target.Nickname}\n" +
                    $"**User ID**: {ev.Target.UserId}\n" +
                    $"**Player ID**: {ev.Target.PlayerId}\n" +
                    $"**Player Role**: {ev.Target.Role.ToString().SpaceByPascalCase()}", false);
            }

            footer.WithText($"🕒 Reported at: {DateTime.Now.ToLocalTime().ToString("G")}");

            embed.WithAuthor(author);
            embed.WithColor(System.Drawing.Color.Orange);
            embed.WithFields(reporterField, reportedField, reasonField);
            embed.WithFooter(footer);
            embed.WithTitle($"⚠️ Player Report");

            foreach (var webhook in WebhookConfig.Webhooks)
            {
                if (webhook.Type is WebhookLog.Report)
                {
                    webhook.Send(null, embed);
                }
            }
        }

        [Event]
        private static void OnCheaterReport(PlayerCheaterReportEvent ev)
        {
            var reporterField = new Discord.DiscordEmbedField();
            var reportedField = new Discord.DiscordEmbedField();
            var reasonField = new Discord.DiscordEmbedField();
            var footer = new Discord.DiscordEmbedFooter();
            var author = new Discord.DiscordEmbedAuthor();
            var embed = new Discord.DiscordEmbed();

            author.WithName($"{ServerConsole._serverName.RemoveHtmlTags()}");

            reasonField.WithName($"❔ Reason");
            reasonField.WithValue($"```{ev.Reason}```");

            reporterField.WithName($"🔗 Reporting Player");
            reportedField.WithName($"🔗 Reported Player");

            if (WebhookConfig.ReportsIncludeIp)
            {
                reporterField.WithValue(
                    $"**Username**: {ev.Player.Nickname}\n" +
                    $"**User ID**: {ev.Player.UserId}\n" +
                    $"**Player IP**: {ev.Player.ReferenceHub.Ip()}\n" +
                    $"**Player ID**: {ev.Player.PlayerId}\n" +
                    $"**Player Role**: {ev.Player.Role.ToString().SpaceByPascalCase()}");

                reportedField.WithValue(
                    $"**Username**: {ev.Target.Nickname}\n" +
                    $"**User ID**: {ev.Target.UserId}\n" +
                    $"**Player IP**: {ev.Target.ReferenceHub.Ip()}\n" +
                    $"**Player ID**: {ev.Target.PlayerId}\n" +
                    $"**Player Role**: {ev.Target.Role.ToString().SpaceByPascalCase()}");
            }
            else
            {
                reporterField.WithValue(
                    $"**Username**: {ev.Player.Nickname}\n" +
                    $"**User ID**: {ev.Player.UserId}\n" +
                    $"**Player ID**: {ev.Player.PlayerId}\n" +
                    $"**Player Role**: {ev.Player.Role.ToString().SpaceByPascalCase()}");

                reportedField.WithValue(
                    $"**Username**: {ev.Target.Nickname}\n" +
                    $"**User ID**: {ev.Target.UserId}\n" +
                    $"**Player ID**: {ev.Target.PlayerId}\n" +
                    $"**Player Role**: {ev.Target.Role.ToString().SpaceByPascalCase()}");
            }

            footer.WithText($"🕒 Reported at: {DateTime.Now.ToLocalTime().ToString("G")}");

            embed.WithAuthor(author);
            embed.WithColor(System.Drawing.Color.Red);
            embed.WithFields(reporterField, reportedField, reasonField);
            embed.WithFooter(footer);
            embed.WithTitle($"🚫 Cheater Report");

            foreach (var webhook in WebhookConfig.Webhooks)
            {
                if (webhook.Type is WebhookLog.Report)
                {
                    webhook.Send(null, embed);
                }
            }
        }
    }
}