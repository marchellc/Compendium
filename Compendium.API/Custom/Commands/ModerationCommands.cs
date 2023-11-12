using BetterCommands;

using Compendium.PlayerData;

using helpers;
using helpers.Time;

using System;
using System.Linq;
using System.Net;

using Compendium.Mutes;

namespace Compendium.Custom.Commands
{
    public static class ModerationCommands
    {
        [Command("oban", CommandType.RemoteAdmin, CommandType.PlayerConsole)]
        [Description("Issues an offline ban.")]
        public static string OfflineBanCommand(ReferenceHub sender, string target, string duration, string reason, bool searchRecords = true)
        {
            if (!TimeUtils.TryParseTime(duration, out var time))
                return $"Failed to parse ban duration!";

            try
            {
                sender.Message(UnbanCommand(sender, target, true), true);
            }
            catch { }

            var idBanIssued = false;
            var ipBanIssued = false;
            var hasRecord = false;

            var now = DateTime.Now;

            var details = new BanDetails()
            {
                Expires = (now + time).Ticks,
                IssuanceTime = now.Ticks,
                Issuer = $"{sender.Nick()} ({sender.UserId()})",
                Reason = reason,
                OriginalName = "Unknown - offline ban"
            };

            PlayerDataRecord record = null;

            if (hasRecord = (PlayerDataRecorder.TryQuery(target, true, out record) && record != null))
                details.OriginalName = record.NameTracking.LastValue;

            if (UserIdValue.TryParse(target, out var userIdValue))
            {
                details.Id = userIdValue.Value;
                BanHandler.IssueBan(details, BanHandler.BanType.UserId);
                idBanIssued = true;
                sender.Message($"Issued user ID ban for '{userIdValue.Value}'", true);
            }
            else if (IPAddress.TryParse(target, out _))
            {
                details.Id = target;
                BanHandler.IssueBan(details, BanHandler.BanType.IP);
                ipBanIssued = true;
                sender.Message($"Issued IP ban for '{details.Id}'", true);
            }
            else if (hasRecord)
            {
                details.Id = record.UserId;
                BanHandler.IssueBan(details, BanHandler.BanType.UserId);
                idBanIssued = true;
                sender.Message($"Issued record-based user ID ban for '{userIdValue.Value}'", true);

                details.Id = record.Ip;
                BanHandler.IssueBan(details, BanHandler.BanType.IP);
                ipBanIssued = true;
                sender.Message($"Issued record-based IP ban for '{details.Id}'", true);
            }
            else
                return $"Failed to parse ID or IP!";

            if (searchRecords && hasRecord)
            {
                if (!idBanIssued)
                {
                    details.Id = record.UserId;
                    BanHandler.IssueBan(details, BanHandler.BanType.UserId);
                    idBanIssued = true;
                    sender.Message($"Issued record-based user ID ban for '{details.Id}' ({record.NameTracking.LastValue})", true);
                }

                if (!ipBanIssued)
                {
                    details.Id = record.Ip;
                    BanHandler.IssueBan(details, BanHandler.BanType.IP);
                    ipBanIssued = true;
                    sender.Message($"Issued record-based IP ban for '{record.Ip}' ({record.NameTracking.LastValue})", true);
                }
            }
            
            if (hasRecord && record.TryGetHub(out var targetHub))
            {
                targetHub.Kick($"You were banned! Reason:\n{reason}");
                sender.Message($"Kicked player {targetHub.Nick()} ({targetHub.UserId()} : {targetHub.Ip()})", true);
            }

            return $"Finished offline banning target '{target}'!";
        }

        [Command("unban", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Unbans a player.")]
        public static string UnbanCommand(ReferenceHub sender, string target, bool removeAll = true)
        {
            if (!removeAll)
            {
                if (UserIdValue.TryParse(target, out var idValue))
                    target = idValue.Value;

                var banQuery = BanHandler.QueryBan(target, target);

                if (banQuery.Key is null && banQuery.Value is null)
                    return $"Failed to find any active bans for target '{target}'";

                if (banQuery.Key != null)
                {
                    BanHandler.RemoveBan(banQuery.Key.Id, BanHandler.BanType.UserId);
                    sender.Message($"Removed ID ban of '{target}'", true);
                }    

                if (banQuery.Value != null)
                {
                    BanHandler.RemoveBan(banQuery.Value.Id, BanHandler.BanType.IP);
                    sender.Message($"Removed IP ban of '{target}'", true);
                }

                return "Done!";
            }

            var targetId = target;
            var targetIp = target;

            if (PlayerDataRecorder.TryQuery(target, true, out var record))
            {
                targetId = record.Id;
                targetIp = record.Ip;

                sender.Message($"Identified target's ID and IP by offline records: {targetId} / {targetIp}", true);
            }
            else
                sender.Message($"Failed to find any offline records for target '{target}'.", true);

            if (targetId != targetIp)
            {
                BanHandler.RemoveBan(targetId, BanHandler.BanType.UserId);
                BanHandler.RemoveBan(targetIp, BanHandler.BanType.IP);

                return $"Removed user ID and IP ban for '{record.NameTracking.LastValue}'.";
            }
            else
            {
                var ipBans = BanHandler.GetBans(BanHandler.BanType.IP);
                var idBans = BanHandler.GetBans(BanHandler.BanType.UserId);

                BanDetails relevantBan = null;

                if (IPAddress.TryParse(target, out _))
                {
                    sender.Message($"Identified target as IP address", true);

                    if (!ipBans.TryGetFirst(b => b.Id == target, out relevantBan))
                        return $"The specified IP address does not have any active IP bans.";
                }
                else if (UserIdValue.TryParse(target, out var userId))
                {
                    sender.Message($"Identified target as user ID", true);

                    if (!idBans.TryGetFirst(b => b.Id == userId.Value, out relevantBan))
                        return $"The specified user ID does not have any active ID bans.";
                }

                if (relevantBan is null)
                    return $"Found no active bans for target '{target}'.";

                var banSum = relevantBan.IssuanceTime + relevantBan.Expires;
                var matchingBans = ipBans.Where(b => banSum == (b.IssuanceTime + b.Expires)
                                                && relevantBan.Issuer == b.Issuer
                                                && relevantBan.Id == b.Id
                                                && relevantBan.OriginalName == b.OriginalName)
                                              .Union(
                                       idBans.Where(b => banSum == (b.IssuanceTime + b.Expires)
                                                && relevantBan.Issuer == b.Issuer
                                                && relevantBan.Id == b.Id
                                                && relevantBan.OriginalName == b.OriginalName)
                                                    ).ToList();

                if (!matchingBans.Any())
                    return $"Found no matching bans.";

                if (!matchingBans.Contains(relevantBan))
                    matchingBans.Add(relevantBan);

                sender.Message($"Identified {matchingBans.Count()} active ban(s).", true);

                matchingBans.ForEach(b =>
                {
                    BanHandler.RemoveBan(b.Id, BanHandler.BanType.IP);
                    BanHandler.RemoveBan(b.Id, BanHandler.BanType.UserId);
                });

                var sb = Pools.PoolStringBuilder(true, "Removed these ban(s):");

                matchingBans.ForEach(b =>
                {
                    sb.AppendLine($"'{b.OriginalName}' {b.Id}, issued at '{new DateTime(b.IssuanceTime).ToLocalTime().ToString("G")}' by '{b.Issuer}' for '{b.Reason}'");
                });

                return sb.ReturnStringBuilderValue();
            }
        }

        [Command("tmute", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Issues a temporary mute.")]
        public static string TemporaryMuteCommand(ReferenceHub sender, ReferenceHub target, string duration, string reason)
        {
            if (!TimeUtils.TryParseTime(duration, out var time))
                return "Failed to parse duration.";

            if (!MuteManager.Issue(sender, target, reason, time))
                return "Failed to issue temporary mute.";

            return $"Issued a temporary mute to '{target.Nick()}' for '{reason}' (expires in {time.UserFriendlySpan()})";
        }

        [Command("tomute", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Issues a temporary offline mute.")]
        public static string TemporaryOfflineMuteCommand(ReferenceHub sender, PlayerDataRecord target, string duration, string reason)
        {
            if (!TimeUtils.TryParseTime(duration, out var time))
                return "Failed to parse duration.";

            if (!MuteManager.Issue(sender, target, reason, time))
                return "Failed to issue temporary mute.";

            return $"Issued a temporary mute to '{target.NameTracking.LastValue}' for '{reason}' (expires in {time.UserFriendlySpan()})";
        }

        [Command("mutes", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Displays a list of active temporary mutes.")]
        public static string MutesCommand(ReferenceHub sender, PlayerDataRecord target)
        {
            var mutes = MuteManager.Query(target);

            if (mutes.Length <= 0)
                return $"{target.NameTracking.LastValue} does not have any active temporary mutes.";

            return $"Active mutes ({mutes.Length}):\n" +
                $"{string.Join("\n", mutes.Select(m => $"[{m.Id}]: Issued by {(PlayerDataRecorder.TryQuery(m.IssuerId, false, out var record) && record.NameTracking.LastValue != null ? $"{record.NameTracking.LastValue} ({record.UserId})" : $"{m.IssuerId}")} for '{m.Reason}' (expires at: {new DateTime(m.ExpiresAt).ToString("G")})"))}";
        }

        [Command("rmute", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Removes a mute using it's ID.")]
        public static string RemoveMuteCommand(ReferenceHub sender, string muteId)
        {
            var mute = MuteManager.Query(muteId);

            if (mute is null)
                return "Failed to find a mute with that ID";

            if (!MuteManager.Remove(mute))
                return "Failed to remove that mute.";

            return "Mute removed.";
        }

        [Command("rmutes", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Removes all mutes for a specified player.")]
        public static string RemoveMutesCommand(ReferenceHub sender, PlayerDataRecord target)
        {
            var mutes = MuteManager.Query(target);

            if (mutes.Length <= 0)
                return $"Player '{target.NameTracking.LastValue}' doesn't have any active mutes.";

            for (int i = 0; i < mutes.Length; i++)
                MuteManager.Remove(mutes[i]);

            return $"Removed {mutes.Length} mute(s).";
        }
    }
}
