﻿using Compendium.Constants;
using Compendium.Events;
using Compendium.Extensions;
using Compendium.Features;
using Compendium.PlayerData;
using Compendium;

using helpers;
using helpers.Extensions;
using helpers.Pooling.Pools;

using PlayerRoles;
using PlayerStatsSystem;

using PluginAPI.Events;

using System;
using System.Linq;

using UnityEngine;

using PluginAPI.Core;

using Compendium.Staff;

using System.Collections.Generic;

namespace Compendium.Webhooks
{
    public class WebhookFeature : ConfigFeatureBase
    {
        private static List<Exception> _loggedFirstChanceExceptions = new List<Exception>();

        public override string Name => "Webhooks";
        public override bool IsPatch => true;

        public override bool CanBeShared => false;

        public static string[] ExceptionSeparator = new string[] { "at" };

        public override void Load()
        {
            base.Load();

            PlayerStats.OnAnyPlayerDamaged += OnDamage;
            PlayerStats.OnAnyPlayerDied += OnDeath;

            AppDomain.CurrentDomain.UnhandledException += (_, ev) =>
            {
                if (ev.ExceptionObject is null || ev.ExceptionObject is not Exception ex)
                    return;

                if (_loggedFirstChanceExceptions.Contains(ex))
                    return;

                var embed = new Discord.DiscordEmbed();
                var typeField = new Discord.DiscordEmbedField();
                var methodField = new Discord.DiscordEmbedField();
                var excField = new Discord.DiscordEmbedField();

                methodField.WithName("❓ Method");
                methodField.WithValue($"```csharp\n{ex.StackTrace.Split(ExceptionSeparator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Unknown"}\n```", false);

                excField.WithName("🗒️ Message");
                excField.WithValue($"```{ex.Message}```", false);

                typeField.WithName("❓ Type");
                typeField.WithValue(ex.GetType().Name, false);

                embed.WithAuthor($"⛔ Caught an unhandled exception");
                embed.WithColor(System.Drawing.Color.Red);
                embed.WithFields(typeField, methodField, excField);
                embed.WithFooter($"🕗 {DateTime.Now.ToString("G")}");

                foreach (var w in WebhookHandler.Webhooks)
                {
                    if (w.Type is WebhookLog.UnhandledExceptions)
                        w.Send(null, embed);
                }
            };
        }

        public static void SendEvent(string msg, WebhookEventLog eventLog)
        {
            foreach (var w in WebhookHandler.Webhooks)
            {
                if (w is WebhookEvent ev)
                {
                    if (ev.AllowedEvents != null && ev.AllowedEvents.Contains(eventLog))
                        ev.Event(msg);
                }
            }
        }

        public static void SendEvent(Discord.DiscordEmbed msg, WebhookEventLog eventLog)
        {
            foreach (var w in WebhookHandler.Webhooks)
            {
                if (w is WebhookEvent ev)
                {
                    if (ev.AllowedEvents != null && ev.AllowedEvents.Contains(eventLog))
                        ev.Event(msg);
                }
            }
        }

        private static string PositionSummary(ReferenceHub target, ReferenceHub attacker)
        {
            var targetRoom = target.RoomId().ToString().SpaceByPascalCase();
            var scps = Hub.Hubs.Where(h => h.IsSCP(true)).OrderBy(h => h.DistanceSquared(target));
            var sb = StringBuilderPool.Pool.Get();

            if (attacker != null && attacker != target)
                sb.AppendLine($"**{attacker.Nick()}**: [{attacker.Zone()}] {attacker.RoomId().ToString().SpaceByPascalCase()}");

            sb.AppendLine($"**{target.Nick()}**: [{target.Zone()}] {targetRoom}");

            scps.ForEach(h =>
            {
                sb.AppendLine($"**{h.RoleId().ToString().SpaceByPascalCase()}**: [{h.Zone()}] {h.RoomId()} *({Mathf.CeilToInt(h.DistanceSquared(target))} units away)*");
            });

            return StringBuilderPool.Pool.PushReturn(sb);
        }

        [Event]
        private static void OnConnection(PlayerPreauthEvent ev)
        {
            SendEvent($"🌐 **{ev.UserId}** is preauthentificating from **{ev.IpAddress}** ({ev.Region}) with flags: {ev.CentralFlags}", WebhookEventLog.PlayerAuth);
        }

        private static void OnDamage(ReferenceHub target, DamageHandlerBase damageHandler)
        {
            if (!RoundHelper.IsStarted)
                return;

            ReferenceHub attacker = null;
            AttackerDamageHandler handler = null;

            if (damageHandler is AttackerDamageHandler)
            {
                handler = damageHandler as AttackerDamageHandler;
                attacker = handler.Attacker.Hub;
            }

            try
            {
                if (attacker is null || attacker == target)
                {
                    SendEvent($"🔫 [{target.RoleId().ToString().SpaceByPascalCase()}] {target.Nick()} ({target.UserId()}) damaged himself using {WebhookHandler.GetDamageName(damageHandler).TrimEnd('.')}.", WebhookEventLog.PlayerSelfDamage);
                }
                else
                {
                    var isTk = handler != null && attacker != null && attacker != target && attacker.GetFaction() == target.GetFaction();

                    if (isTk)
                    {
                        SendEvent(new Discord.DiscordEmbed()
                            .WithTitle($"⚠️ Team Damage")
                            .WithField("🔗 Attacker", $"{attacker.RoleId().ToString().SpaceByPascalCase()} **{attacker.Nick()}** ({attacker.UserId()})", false)
                            .WithField("🔗 Target", $"{target.RoleId().ToString().SpaceByPascalCase()} **{target.Nick()}** ({target.UserId()})", false)
                            .WithField("🔗 Damage", $"**{Mathf.CeilToInt(handler.Damage)} HP** ({WebhookHandler.GetDamageName(damageHandler).TrimEnd('.')})", false)
                            .WithField("🌐 Position Summary", PositionSummary(target, attacker), false)
                            .WithColor(System.Drawing.Color.Orange), WebhookEventLog.PlayerFriendlyDamage);
                    }
                    else if (attacker != null)
                    {
                        SendEvent($"🔫 [{attacker.RoleId().ToString().SpaceByPascalCase()}] {attacker.Nick()} ({attacker.UserId()}) damaged player {target.RoleId().ToString().SpaceByPascalCase()} {target.Nick()} ({target.UserId()}) with {WebhookHandler.GetDamageName(damageHandler).TrimEnd('.')}", WebhookEventLog.PlayerDamage);
                    }
                    else
                    {
                        SendEvent($"🔫 [{target.RoleId().ToString().SpaceByPascalCase()}] {target.Nick()} ({target.UserId()}) damaged himself using {WebhookHandler.GetDamageName(damageHandler).TrimEnd('.')}.", WebhookEventLog.PlayerSelfDamage);
                    }
                }
            }
            catch (Exception ex)
            {
                FLog.Error($"OnDamage caught exception: {ex.Message}");
            }
        }

        private static void OnDeath(ReferenceHub target, DamageHandlerBase damageHandler)
        {
            if (!RoundHelper.IsStarted)
                return;

            ReferenceHub attacker = null;
            AttackerDamageHandler handler = null;

            if (damageHandler is AttackerDamageHandler)
            {
                handler = damageHandler as AttackerDamageHandler;
                attacker = handler.Attacker.Hub;
            }

            try
            {
                if (attacker is null || attacker == target)
                {
                    SendEvent($"🪦 [{target.RoleId().ToString().SpaceByPascalCase()}] {target.Nick()} ({target.UserId()}) killed himself using {WebhookHandler.GetDamageName(damageHandler).TrimEnd('.')}.", WebhookEventLog.PlayerSuicide);
                }
                else
                {
                    var isTk = handler != null && attacker != null && attacker != target && attacker.GetFaction() == target.GetFaction();

                    if (isTk)
                    {
                        SendEvent(new Discord.DiscordEmbed()
                            .WithTitle($"☠️ Team Kill")
                            .WithField("🔗 Attacker", $"{attacker.RoleId().ToString().SpaceByPascalCase()} **{attacker.Nick()}** ({attacker.UserId()})", false)
                            .WithField("🔗 Target", $"{target.RoleId().ToString().SpaceByPascalCase()} **{target.Nick()}** ({target.UserId()})", false)
                            .WithField("🔗 Damage", $"**{Mathf.CeilToInt(handler.Damage)} HP** ({WebhookHandler.GetDamageName(damageHandler).TrimEnd('.')})", false)
                            .WithField("🌐 Position Summary", PositionSummary(target, attacker), false)
                            .WithColor(System.Drawing.Color.Red), WebhookEventLog.PlayerFriendlyKill);
                    }
                    else if (attacker != null)
                    {
                        SendEvent($"💀 [{attacker.RoleId().ToString().SpaceByPascalCase()}] {attacker.Nick()} ({attacker.UserId()}) killed player {target.RoleId().ToString().SpaceByPascalCase()} {target.Nick()} ({target.UserId()}) with {WebhookHandler.GetDamageName(damageHandler).TrimEnd('.')}", WebhookEventLog.PlayerKill);
                    }
                    else
                    {
                        SendEvent($"🪦 [{target.RoleId().ToString().SpaceByPascalCase()}] {target.Nick()} ({target.UserId()}) killed himself using {WebhookHandler.GetDamageName(damageHandler).TrimEnd('.')}.", WebhookEventLog.PlayerSelfDamage);
                    }
                }
            }
            catch (Exception ex)
            {
                FLog.Error($"OnDeath caught exception: {ex.Message}");
            }
        }

        [Event]
        private static void OnJoined(PlayerJoinedEvent ev)
        {
            Calls.Delay(0.5f, () => SendEvent($"➡️ **{ev.Player.Nickname} ({ev.Player.UserId}) joined from {ev.Player.IpAddress}** (assigned unique ID: **{ev.Player.ReferenceHub.UniqueId()}**)", WebhookEventLog.PlayerJoined));
        }

        [Event]
        private static void OnLeft(PlayerLeftEvent ev)
        {
            SendEvent($"⬅️ **{ev.Player.Nickname} ({ev.Player.UserId}) left from {ev.Player.IpAddress}** (assigned unique ID: **{ev.Player.ReferenceHub.UniqueId()}**)", WebhookEventLog.PlayerLeft);
        }

        [Event]
        private static void OnCuffed(PlayerHandcuffEvent ev)
        {
            SendEvent($"🔒 [{ev.Player.Role.ToString().SpaceByPascalCase()}] {ev.Player.Nickname} ({ev.Player.UserId}) cuffed player {ev.Target.Role.ToString().SpaceByPascalCase()} {ev.Target.Nickname} ({ev.Target.UserId}) with {ev.Player.CurrentItem?.ItemTypeId.ToString().SpaceByPascalCase() ?? "Unknown"}", WebhookEventLog.PlayerCuff);
        }

        [Event]
        private static void OnUncuffed(PlayerRemoveHandcuffsEvent ev)
        {
            SendEvent($"🔓 [{ev.Player.Role.ToString().SpaceByPascalCase()}] {ev.Player.Nickname} ({ev.Player.UserId}) uncuffed player {ev.Target.Role.ToString().SpaceByPascalCase()} {ev.Target.Nickname} ({ev.Target.UserId})", WebhookEventLog.PlayerUncuff);
        }

        [Event]
        private static void OnThrownGrenade(PlayerThrowProjectileEvent ev)
        {
            if (ev.Item.Category != ItemCategory.Grenade)
                return;

            SendEvent($"🕓 [{ev.Thrower.Role.ToString().SpaceByPascalCase()}] {ev.Thrower.Nickname} ({ev.Thrower.UserId}) threw their {ev.Item.ItemTypeId.ToString().SpaceByPascalCase()}", WebhookEventLog.GrenadeThrown);
        }

        [Event]
        private static void OnGrenadeExploded(GrenadeExplodedEvent ev)
        {
            if (ev.Grenade is null || ev.Grenade.PreviousOwner.Role.GetFaction() is Faction.SCP)
                return;

            SendEvent($"💥 [{ev.Grenade.PreviousOwner.Role.ToString().SpaceByPascalCase()}] {ev.Grenade.PreviousOwner.Nickname} ({ev.Grenade.PreviousOwner.LogUserID})'s {ev.Grenade.Info.ItemId.ToString().SpaceByPascalCase().Remove("Grenade").Trim()} grenade exploded.", WebhookEventLog.GrenadeExploded);
        }

        [Event]
        private static void OnBanned(BanIssuedEvent ev)
        {
            if (ev.BanType != BanHandler.BanType.UserId)
                return;

            var issuedDate = new DateTime(ev.BanDetails.IssuanceTime);
            var expiresDate = new DateTime(ev.BanDetails.Expires);
            var duration = (int)Math.Floor(TimeSpan.FromTicks((expiresDate - issuedDate).Ticks).TotalSeconds);

            var targetNick = "Unknown Nick";
            var targetId = "Unknown ID";
            var targetIp = "Unknown IP";

            try
            {
                if (PlayerDataRecorder.TryQuery(ev.BanDetails.Id, false, out var targetRecord))
                {
                    targetNick = targetRecord.NameTracking.LastValue;
                    targetId = targetRecord.UserId;
                    targetIp = targetRecord.Ip;
                }
            }
            catch (Exception ex)
            {
                FLog.Error($"Caught an exception when trying to parse ban details!");
                FLog.Error(ex);
            }

            foreach (var webhook in WebhookHandler.Webhooks)
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
                    var reason = string.IsNullOrWhiteSpace(ev.BanDetails.Reason) ? "No reason provided." : ev.BanDetails.Reason;

                    author.WithName(World.CurrentClearOrAlternativeServerName);

                    reasonField.WithName($"❔ Reason");
                    reasonField.WithValue($"```{reason}```", false);

                    issuerField.WithName($"🔗 Issuer");
                    bannedField.WithName($"🔗 Player");

                    durationField.WithName($"🕒 Duration");
                    durationField.WithValue($"{TimeUtils.SecondsToCompoundTime(duration)}", false);

                    if (WebhookHandler.PrivateBansIncludeIp)
                    {
                        issuerField.WithValue(ev.BanDetails.Issuer, false);
                        bannedField.WithValue(
                            $"**Username**: {targetNick}\n" +
                            $"**User ID**: {targetId}\n" +
                            $"**User IP**: {targetIp}", false);
                    }
                    else
                    {
                        issuerField.WithValue(ev.BanDetails.Issuer, false);
                        bannedField.WithValue(
                            $"**Username**: {targetNick}\n" +
                            $"**User ID**: {targetId}\n", false);
                    }

                    footer.WithText(
                        $"🕒 Banned at: {issuedDate.ToLocalTime().ToString("G")}\n" +
                        $"🕒 Expires at: {expiresDate.ToLocalTime().ToString("G")}");

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
                    var reason = string.IsNullOrWhiteSpace(ev.BanDetails.Reason) ? "No reason provided." : ev.BanDetails.Reason;

                    author.WithName(World.CurrentClearOrAlternativeServerName);

                    reasonField.WithName($"❔ Reason");
                    reasonField.WithValue($"```{reason}```", false);

                    issuerField.WithName($"🔗 Issuer");
                    bannedField.WithName($"🔗 Player");

                    durationField.WithName($"🕒 Duration");
                    durationField.WithValue($"{TimeUtils.SecondsToCompoundTime(duration)}", false);

                    issuerField.WithValue(ev.BanDetails.Issuer, false);
                    bannedField.WithValue($"**{targetNick}**", false);

                    footer.WithText(
                        $"🕒 Banned at: {issuedDate.ToLocalTime().ToString("G")}\n" +
                        $"🕒 Expires at: {expiresDate.ToLocalTime().ToString("G")}");

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
            if (WebhookHandler.AnnounceReportsInGame)
            {
                Hub.Hubs.ForEach(h =>
                {
                    if (!h.IsStaff(false))
                        return;

                    h.Hint(
                        $"<b><color={Colors.RedValue}>[REPORT]</color></b>\n" +
                        $"Hráč <b><color={Colors.GreenValue}>{ev.Player.Nickname}</color></b> nahlásil hráče <b><color={Colors.GreenValue}>{ev.Target.Nickname}</color></b> za:\n" +
                        $"<b><color={Colors.LightGreenValue}>{ev.Reason}</color></b>",
                        10f);
                });
            }

            var reporterField = new Discord.DiscordEmbedField();
            var reportedField = new Discord.DiscordEmbedField();
            var reasonField = new Discord.DiscordEmbedField();
            var footer = new Discord.DiscordEmbedFooter();
            var author = new Discord.DiscordEmbedAuthor();
            var embed = new Discord.DiscordEmbed();

            author.WithName(World.CurrentClearOrAlternativeServerName);

            reasonField.WithName($"❔ Reason");
            reasonField.WithValue($"```{ev.Reason}```", false);

            reporterField.WithName($"🔗 Reporting Player");
            reportedField.WithName($"🔗 Reported Player");

            if (WebhookHandler.ReportsIncludeIp)
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

            foreach (var webhook in WebhookHandler.Webhooks)
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
            if (WebhookHandler.AnnounceReportsInGame)
            {
                Hub.Hubs.ForEach(h =>
                {
                    if (!h.IsStaff())
                        return;

                    h.Hint(
                        $"<b><color={Colors.RedValue}>[CHEATER REPORT]</color></b>\n" +
                        $"Hráč <b><color={Colors.GreenValue}>{ev.Player.Nickname}</color></b> nahlásil hráče <b><color={Colors.GreenValue}>{ev.Target.Nickname}</color></b> za:\n" +
                        $"<b><color={Colors.LightGreenValue}>{ev.Reason}</color></b>",
                        10f);
                });
            }

            var reporterField = new Discord.DiscordEmbedField();
            var reportedField = new Discord.DiscordEmbedField();
            var reasonField = new Discord.DiscordEmbedField();
            var footer = new Discord.DiscordEmbedFooter();
            var author = new Discord.DiscordEmbedAuthor();
            var embed = new Discord.DiscordEmbed();

            author.WithName(World.CurrentClearOrAlternativeServerName);

            reasonField.WithName($"❔ Reason");
            reasonField.WithValue($"```{ev.Reason}```");

            reporterField.WithName($"🔗 Reporting Player");
            reportedField.WithName($"🔗 Reported Player");

            if (WebhookHandler.ReportsIncludeIp)
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

            foreach (var webhook in WebhookHandler.Webhooks)
            {
                if (webhook.Type is WebhookLog.Report)
                {
                    webhook.Send(null, embed);
                }
            }
        }

        [Event]
        private static void OnPlayerCommand(PlayerGameConsoleCommandExecutedEvent ev)
        {
            ReferenceHub sender = null;

            if (ev.Player is null)
                sender = ReferenceHub.HostHub;
            else
                sender = ev.Player.ReferenceHub;

            if (sender is null)
                return;

            var senderField = new Discord.DiscordEmbedField();
            var commandField = new Discord.DiscordEmbedField();
            var resultField = new Discord.DiscordEmbedField();
            var author = new Discord.DiscordEmbedAuthor();
            var embed = new Discord.DiscordEmbed();
            var hasGroup = StaffHandler.Members.TryGetValue(sender.UserId(), out var groupKeys) && groupKeys != null && groupKeys.Length > 0;
            var groups = hasGroup ? groupKeys.Select(g => StaffHandler.Groups.TryGetValue(g, out var gr) ? gr : null).Where(g => g != null).OrderBy(c => c.Permissions.Count).ToArray() : Array.Empty<StaffGroup>();

            author.WithName(World.CurrentClearOrAlternativeServerName);

            senderField.WithName("❓ Sender");
            senderField.WithValue($"**{sender.Nick()}** ({sender.ParsedUserId().ClearId}){(groups.Length > 0 ? $" ({string.Join(" | ", groups.Select(g => g.Text))}" : "")})", false);

            commandField.WithName("🗒️ Command");
            commandField.WithValue($"```{ev.Command} '{string.Join(" ", ev.Arguments)}'```", false);

            resultField.WithName("➡️ Response");
            resultField.WithValue($"```{ev.Response.Remove($"{ev.Command.ToUpperInvariant()}#")}```", false);

            embed.WithTitle($"{(ev.Result ? "✅" : "⛔")} Player Console Command Executed");
            embed.WithAuthor(author);
            embed.WithFields(senderField, commandField, resultField);
            embed.WithColor(ev.Result ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            embed.WithFooter($"🕗 {DateTime.Now.ToString("G")}");

            foreach (var webhook in WebhookHandler.Webhooks)
            {
                if (webhook.Type is WebhookLog.GameCommands)
                    webhook.Send(null, embed);
            }
        }

        [Event]
        private static void OnConsoleCommmand(ConsoleCommandExecutedEvent ev)
        {
            var commandField = new Discord.DiscordEmbedField();
            var resultField = new Discord.DiscordEmbedField();
            var author = new Discord.DiscordEmbedAuthor();
            var embed = new Discord.DiscordEmbed();

            author.WithName(World.CurrentClearOrAlternativeServerName);

            commandField.WithName("🗒️ Command");
            commandField.WithValue($"```{ev.Command} '{string.Join(" ", ev.Arguments)}'```", false);

            resultField.WithName("➡️ Response");
            resultField.WithValue($"```{ev.Response.Remove($"{ev.Command.ToUpperInvariant()}#")}```", false);

            embed.WithTitle($"{(ev.Result ? "✅" : "⛔")} Console Command Executed");
            embed.WithAuthor(author);
            embed.WithFields(commandField, resultField);
            embed.WithColor(ev.Result ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            embed.WithFooter($"🕗 {DateTime.Now.ToString("G")}");

            foreach (var webhook in WebhookHandler.Webhooks)
            {
                if (webhook.Type is WebhookLog.ConsoleCommands)
                    webhook.Send(null, embed);
            }
        }

        [Event]
        private static void OnRemoteCommand(RemoteAdminCommandExecutedEvent ev)
        {
            ReferenceHub sender = null;

            if (!Player.TryGet(ev.Sender, out var player))
                sender = ReferenceHub.HostHub;
            else
                sender = player.ReferenceHub;

            if (sender is null)
                return;

            var senderField = new Discord.DiscordEmbedField();
            var commandField = new Discord.DiscordEmbedField();
            var resultField = new Discord.DiscordEmbedField();
            var author = new Discord.DiscordEmbedAuthor();
            var embed = new Discord.DiscordEmbed();
            var hasGroup = StaffHandler.Members.TryGetValue(sender.UserId(), out var groupKeys) && groupKeys != null && groupKeys.Length > 0;
            var groups = hasGroup ? groupKeys.Select(g => StaffHandler.Groups.TryGetValue(g, out var gr) ? gr : null).Where(g => g != null).OrderBy(c => c.Permissions.Count).ToArray() : Array.Empty<StaffGroup>();

            author.WithName(World.CurrentClearOrAlternativeServerName);

            senderField.WithName("❓ Sender");
            senderField.WithValue($"**{sender.Nick()}** ({sender.ParsedUserId().ClearId}){(groups.Length > 0 ? $" ({string.Join(" | ", groups.Select(g => g.Text))}" : "")})", false);

            commandField.WithName("🗒️ Command");
            commandField.WithValue($"```{ev.Command} '{string.Join(" ", ev.Arguments)}'```", false);

            resultField.WithName("➡️ Response");
            resultField.WithValue($"```{ev.Response.Remove($"{ev.Command.ToUpperInvariant()}#")}```", false);

            embed.WithTitle($"{(ev.Result ? "✅" : "⛔")} Remote Admin Command Executed");
            embed.WithAuthor(author);
            embed.WithFields(senderField, commandField, resultField);
            embed.WithColor(ev.Result ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            embed.WithFooter($"🕗 {DateTime.Now.ToString("G")}");

            foreach (var webhook in WebhookHandler.Webhooks)
            {
                if (webhook.Type is WebhookLog.RaCommands)
                    webhook.Send(null, embed);
            }    
        }
    }
}