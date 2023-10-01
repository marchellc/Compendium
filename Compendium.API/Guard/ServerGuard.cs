using BetterCommands;

using Compendium.Events;
using Compendium.Guard.Steam;
using Compendium.Guard.Vpn;
using Compendium.UserId;

using helpers.Attributes;
using helpers.IO.Storage;

using PluginAPI.Events;

using System.Net;
using System;

namespace Compendium.Guard
{
    public static class ServerGuard
    {
        private static SingleFileStorage<string> _cleanIdStorage;
        private static SingleFileStorage<string> _cleanIpStorage;

        private static SingleFileStorage<string> _flaggedIdStorage;
        private static SingleFileStorage<string> _flaggedIpStorage;

        public static VpnClient Vpn { get; } = new VpnClient();
        public static SteamClient Steam { get; } = new SteamClient();

        [Load]
        [Reload]
        public static void Load()
        {
            try
            {
                if (_cleanIdStorage is null)
                    _cleanIdStorage = new SingleFileStorage<string>($"{Directories.ThisData}/SavedGuardCleanIDs");

                if (_cleanIpStorage is null)
                    _cleanIpStorage = new SingleFileStorage<string>($"{Directories.ThisData}/SavedGuardCleanIPs");

                if (_flaggedIdStorage is null)
                    _flaggedIdStorage = new SingleFileStorage<string>($"{Directories.ThisData}/SavedGuardFlaggedIDs");

                if (_flaggedIpStorage is null)
                    _flaggedIpStorage = new SingleFileStorage<string>($"{Directories.ThisData}/SavedGuardFlaggedIPs");

                _cleanIdStorage.Load();
                _cleanIpStorage.Load();

                _flaggedIdStorage.Load();
                _flaggedIpStorage.Load();

                Vpn.Reload();
                Steam.Reload();

                Plugin.Info($"Server Guard loaded.");
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

            if (_cleanIdStorage.Contains(ev.UserId) || _cleanIpStorage.Contains(ev.IpAddress))
                return;

            if (_flaggedIdStorage.Contains(ev.UserId) || _flaggedIpStorage.Contains(ev.IpAddress))
            {
                ev.ConnectionRequest.Result = LiteNetLib.ConnectionRequestResult.RejectForce;
                ev.ConnectionRequest.RejectForce();

                Plugin.Warn($"Rejected connection request of previously flagged user: {ev.UserId} {ev.IpAddress}");
            }
        }

        public static void Flag(string userId, string ip)
        {
            if (!string.IsNullOrWhiteSpace(userId) && !_flaggedIdStorage.Contains(userId))
                _flaggedIdStorage.Add(userId);

            if (!string.IsNullOrWhiteSpace(ip) && !_flaggedIpStorage.Contains(ip))
                _flaggedIpStorage.Add(ip);

            if (!string.IsNullOrWhiteSpace(userId) && _cleanIdStorage.Contains(userId))
                _cleanIdStorage.Remove(userId);

            if (!string.IsNullOrWhiteSpace(ip) && _cleanIpStorage.Contains(ip))
                _cleanIpStorage.Remove(ip);
        }

        public static void Safe(string userId, string ip)
        {
            if (!string.IsNullOrWhiteSpace(userId) && !_cleanIdStorage.Contains(userId))
                _cleanIdStorage.Add(userId);

            if (!string.IsNullOrWhiteSpace(ip) && !_cleanIpStorage.Contains(ip))
                _cleanIpStorage.Add(ip);

            if (!string.IsNullOrWhiteSpace(userId) && _flaggedIdStorage.Contains(userId))
                _flaggedIdStorage.Remove(userId);

            if (!string.IsNullOrWhiteSpace(ip) && _flaggedIpStorage.Contains(ip))
                _flaggedIpStorage.Remove(ip);
        }

        public static bool IsClean(ReferenceHub hub)
        {
            if (hub is null)
                return true;

            if (_cleanIdStorage.Contains(hub.UserId()) || _cleanIpStorage.Contains(hub.Ip()))
                return true;

            if (hub.IsNorthwoodModerator() || hub.IsNorthwoodStaff())
                return true;

            return false;
        }

        [Command("safeid", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Marks the specified user ID as safe.")]
        private static string SafeIdCommand(ReferenceHub sender, string userId)
        {
            if (!UserIdHelper.TryParse(userId, out var uid))
                return "Invalid User ID.";

            Safe(uid.FullId, "");
            return $"Marked user ID '{uid.FullId}' as safe.";
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
            bool isFlaggedIp = _flaggedIpStorage.Contains(query);
            bool isFlaggedId = UserIdHelper.TryParse(query, out var uid) && _flaggedIdStorage.Contains(uid.FullId);
            bool isCleanIp = _cleanIpStorage.Contains(query);
            bool isCleanId = UserIdHelper.TryParse(query, out uid) && _cleanIdStorage.Contains(uid.FullId);

            return $"Guard status of '{query}'\n" +
                $"IP flag: {isFlaggedIp}\n" +
                $"ID flag: {isFlaggedId}\n" +
                $"Clean IP: {isCleanIp}\n" +
                $"Clean ID: {isCleanId}";
        }
    }
}