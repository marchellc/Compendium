using BetterCommands;
using BetterCommands.Permissions;

using Compendium.Features;
using Compendium.ServerGuard.Dispatch;

using helpers.Configuration.Ini;
using helpers.IO.Storage;
using helpers.Json;

using PluginAPI.Core;

using System;
using System.Collections.Generic;

using Compendium.Logging;

namespace Compendium.ServerGuard.VpnShield
{
    public static class VpnShieldHandler
    {
        private static SingleFileStorage<VpnShieldData> _vpnCache;
        private static KeyValuePair<string, string>? _keyHeader;

        [IniConfig(Name = "VPN Enabled", Description = "Whether or not to enable VPN checks.")]
        public static bool VpnEnabled { get; set; } = true;

        [IniConfig(Name = "VPN Strict Mode", Description = "Whether or not to use strict filtering - this may result in more false positives.")]
        public static bool VpnStrict { get; set; } = true;

        [IniConfig(Name = "VPN Key", Description = "A key for iphub")]
        public static string VpnKey
        {
            get
            {
                if (!_keyHeader.HasValue)
                    return "default";

                return _keyHeader.Value.Value;
            }
            set
            {
                _keyHeader = new KeyValuePair<string, string>("X-Key", value);
                FLog.Debug($"Updated X-Key header: {_keyHeader.Value}");
            }
        }

        [IniConfig(Name = "Blocked ASNs", Description = "A list of blocked ASN IDs.")]
        public static List<int> BlockedAsnList { get; set; } = new List<int>();

        [IniConfig(Name = "Blocked Providers", Description = "A list of blocked internet service providers.")]
        public static List<string> BlockedProviders { get; set; } = new List<string>();

        public static void Initialize()
        {
            _vpnCache = new SingleFileStorage<VpnShieldData>($"{FeatureManager.DirectoryPath}/vpn_cache");
            _vpnCache.Load();
        }

        public static void Check(ReferenceHub hub, Action<bool> callback)
        {
            if (!VpnEnabled)
            {
                callback(false);
                return;
            }

            if (!_keyHeader.HasValue || string.IsNullOrWhiteSpace(VpnKey) || VpnKey is "default")
            {
                callback(false);
                return;
            }

            if (hub.serverRoles.Staff || hub.serverRoles.RaEverywhere)
            {
                callback(false);
                return;
            }

            if (_vpnCache.TryGet(d => 
                       d.Ip == hub.connectionToClient.address 
                    || d.UserId == hub.characterClassManager.UserId 
                    || d.Token == hub.characterClassManager.AuthToken, out var data))
            {
                var hasChanged = false;

                if (string.IsNullOrWhiteSpace(data.Ip) || data.Ip != hub.connectionToClient.address)
                {
                    data.Ip = hub.connectionToClient.address;
                    hasChanged = true;
                }

                if (string.IsNullOrWhiteSpace(data.UserId) || data.UserId != hub.characterClassManager.UserId)
                {
                    data.UserId = hub.characterClassManager.UserId;
                    hasChanged = true;
                }

                if (string.IsNullOrWhiteSpace(data.Token) || data.Token != hub.characterClassManager.AuthToken)
                {
                    data.Token = hub.characterClassManager.AuthToken;
                    hasChanged = true;
                }

                if (hasChanged)
                    _vpnCache.Save();

                if (data.Flags != VpnShieldFlags.Clean)
                {
                    FLog.Warn($"Kicked {hub.LoggedNameFromRefHub()} ({hub.connectionToClient.address}) ({hub.characterClassManager.AuthTokenSerial}) - cached detection.");
                    Kick(hub, "VPN or proxy network detected.");
                    callback(true);
                    return;
                }
                else
                {
                    callback(false);
                    return;
                }
            }

            HttpDispatch.Queue($"http://v2.api.iphub.info/ip/{hub.connectionToClient.address}", json =>
            {
                try
                {
                    var response = JsonHelper.FromJson<VpnResponse>(json);

                    if (response != null)
                    {
                        _vpnCache.Add(new VpnShieldData()
                        {
                            Flags = response.Flags,
                            Ip = hub.connectionToClient.address,
                            Token = hub.characterClassManager.AuthToken,
                            UserId = hub.characterClassManager.UserId
                        });

                        _vpnCache.Save();

                        if (response.Flags != VpnShieldFlags.Clean)
                        {
                            FLog.Warn($"Kicked {hub.LoggedNameFromRefHub()} ({hub.connectionToClient.address}) ({hub.characterClassManager.AuthTokenSerial}) - VPN/proxy detected.");
                            Kick(hub, "VPN / proxy detected");
                            callback(true);
                        }
                        else if (BlockedAsnList.Contains(response.AsnId))
                        {
                            FLog.Warn($"Kicked {hub.LoggedNameFromRefHub()} ({hub.connectionToClient.address}) ({hub.characterClassManager.AuthTokenSerial}) - blacklisted ASN.");
                            Kick(hub, "Blacklisted ASN");
                            callback(true);
                        }
                        else if (BlockedProviders.Contains(response.Provider))
                        {
                            FLog.Warn($"Kicked {hub.LoggedNameFromRefHub()} ({hub.connectionToClient.address}) ({hub.characterClassManager.AuthTokenSerial}) - blacklisted provider.");
                            Kick(hub, "Blacklisted Provider");
                            callback(true);
                        }
                        else
                        {
                            callback(false);
                        }
                    }
                }
                catch { }
            }, _keyHeader.Value);
        }

        private static void Kick(ReferenceHub hub, string reason)
            => ServerConsole.Disconnect(hub.connectionToClient, $"Kicked by VPN shield: {reason}");

        [Command("vpnwhitelist", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("vpnwh")]
        [Permission(PermissionNodeMode.AnyOf, "guard.vpn.whitelist")]
        private static string WhitelistCommand(Player sender, string ip)
        {
            if (_vpnCache.TryGet(d => d.Ip == ip, out var data))
            {
                if (data.Flags == VpnShieldFlags.Clean)
                {
                    _vpnCache.Remove(data);
                    _vpnCache.Save();

                    return $"{data.Ip} ({data.UserId}) IP whitelist removed.";
                }

                data.Flags = VpnShieldFlags.Clean;

                _vpnCache.Save();
                return $"IP {ip} succesfully whitelisted.";
            }

            _vpnCache.Add(new VpnShieldData
            {
                Flags = VpnShieldFlags.Clean,
                Ip = ip,
                Token = "empty",
                UserId = "empty"
            });

            _vpnCache.Save();
            return $"IP {ip} succesfully whitelisted.";
        }
    }
}