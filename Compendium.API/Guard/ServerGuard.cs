using BetterCommands;

using Compendium.Events;
using Compendium.Guard.Steam;
using Compendium.Guard.Vpn;
using Compendium.IO.Saving;

using helpers.Attributes;

using PluginAPI.Events;

using System.Net;
using System;

namespace Compendium.Guard
{
    public static class ServerGuard
    {
        public static SaveFile<CollectionSaveData<string>> Flagged;
        public static SaveFile<CollectionSaveData<string>> Clean;

        public static VpnClient Vpn { get; } = new VpnClient();
        public static SteamClient Steam { get; } = new SteamClient();

        [Load]
        [Reload]
        public static void Load()
        {
            try
            {
                Flagged = new SaveFile<CollectionSaveData<string>>(Directories.GetDataPath("ServerGuardFlags", "guardFlags"));
                Clean = new SaveFile<CollectionSaveData<string>>(Directories.GetDataPath("ServerGuardClean", "guardClean"));

                Vpn.Reload();
                Steam.Reload();
            }
            catch (Exception ex)
            {
                Plugin.Error(ex);
            }
        }

        [Event(Priority = helpers.Priority.Lowest)]
        public static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (IsClean(ev.Player.ReferenceHub))
                return;

            if (Plugin.Config.GuardSettings.VpnClientKey != "none")
                Vpn.TryCheck(ev.Player.ReferenceHub);
        }

        [Event(Priority = helpers.Priority.Highest)]
        public static void OnPreauthentificating(PlayerPreauthEvent ev)
        {
            if (ev.CentralFlags.HasFlagFast(CentralAuthPreauthFlags.NorthwoodStaff)
                || ev.CentralFlags.HasFlagFast(CentralAuthPreauthFlags.IgnoreBans)
                || ev.CentralFlags.HasFlagFast(CentralAuthPreauthFlags.IgnoreGeoblock)
                || ev.CentralFlags.HasFlagFast(CentralAuthPreauthFlags.IgnoreWhitelist))
            {
                Plugin.Debug($"Ignoring preauth request - flags {ev.CentralFlags}");
                return;
            }

            if (Clean.Data.Contains(ev.UserId) || Clean.Data.Contains(ev.IpAddress))
                return;

            if (Flagged.Data.Contains(ev.UserId) || Flagged.Data.Contains(ev.IpAddress))
            {
                ev.ConnectionRequest.Result = LiteNetLib.ConnectionRequestResult.RejectForce;
                ev.ConnectionRequest.RejectForce();

                Plugin.Warn($"Rejected connection request of previously flagged user: {ev.UserId} {ev.IpAddress}");
            }
        }

        public static void Flag(string userId, string ip)
        {
            var ipFlag = false;
            var idFlag = false;

            if (!string.IsNullOrWhiteSpace(userId) && Clean.Data.Contains(userId))
            {
                Clean.Data.Remove(userId);
                idFlag = true;
            }

            if (!string.IsNullOrWhiteSpace(ip) && Clean.Data.Contains(ip))
            {
                Clean.Data.Remove(ip);
                ipFlag = true;
            }

            if (ipFlag || idFlag)
                Clean.Save();

            if (!string.IsNullOrWhiteSpace(userId) && !Flagged.Data.Contains(userId))
            {
                Flagged.Data.Add(userId);
                idFlag = true;
            }

            if (!string.IsNullOrWhiteSpace(ip) && !Flagged.Data.Contains(ip))
            {
                Flagged.Data.Add(ip);
                ipFlag = true;
            }

            if (ipFlag || idFlag)
                Flagged.Save();
        }

        public static void Safe(string userId, string ip)
        {
            var ipFlag = false;
            var idFlag = false;

            if (!string.IsNullOrWhiteSpace(userId) && !Clean.Data.Contains(userId))
            {
                Clean.Data.Add(userId);
                idFlag = true;
            }

            if (!string.IsNullOrWhiteSpace(ip) && !Clean.Data.Contains(ip))
            {
                Clean.Data.Add(ip);
                ipFlag = true;
            }

            if (ipFlag || idFlag)
                Clean.Save();

            if (!string.IsNullOrWhiteSpace(userId) && Flagged.Data.Contains(userId))
            {
                Flagged.Data.Remove(userId);
                idFlag = true;
            }

            if (!string.IsNullOrWhiteSpace(ip) && Flagged.Data.Contains(ip))
            {
                Flagged.Data.Remove(ip);
                ipFlag = true;
            }

            if (ipFlag || idFlag)
                Flagged.Save();
        }

        public static bool IsClean(ReferenceHub hub)
        {
            if (hub is null)
                return true;

            if (hub.IsNorthwoodModerator() || hub.IsNorthwoodStaff())
                return true;

            return Clean.Data.Contains(hub.UserId()) || Clean.Data.Contains(hub.Ip());
        }

        [Command("safeid", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Marks the specified user ID as safe.")]
        private static string SafeIdCommand(ReferenceHub sender, string userId)
        {
            if (!UserIdValue.TryParse(userId, out var uid))
                return "Invalid User ID.";

            Safe(uid.Value, null);
            return $"Marked user ID '{uid.Value}' as safe.";
        }

        [Command("safeip", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Marks the specified IP as safe.")]
        private static string SafeIpCommand(ReferenceHub sender, string ip)
        {
            if (!IPAddress.TryParse(ip, out _))
                return "Invalid IP address.";

            Safe(null, ip);

            return $"Marked IP '{ip}' as safe.";
        }

        [Command("guard", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Views all available server guard info on the specified target.")]
        private static string GuardCommand(ReferenceHub sender, string query)
        {
            var isFlaggedIp = Flagged.Data.Contains(query);
            var isFlaggedId = UserIdValue.TryParse(query, out var uid) && Flagged.Data.Contains(uid.Value);

            var isCleanIp = Clean.Data.Contains(query);
            var isCleanId = UserIdValue.TryParse(query, out uid) && Clean.Data.Contains(uid.Value);

            return $"Guard status of '{query}'\n" +
                $"IP flag: {isFlaggedIp}\n" +
                $"ID flag: {isFlaggedId}\n\n" +
                $"Clean IP: {isCleanIp}\n" +
                $"Clean ID: {isCleanId}";
        }
    }
}