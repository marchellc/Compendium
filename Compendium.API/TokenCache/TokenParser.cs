using helpers.Extensions;
using helpers.Json;

using System;

namespace Compendium.TokenCache
{
    public static class TokenParser
    {
        public static readonly string[] TokenSplitArray = new string[] { "<br>" };

        public static bool TryParse(string token, out TokenData tokenData)
        {
            tokenData = null;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                if (token.Contains("<br>"))
                {
                    var lines = token.Split(TokenSplitArray, StringSplitOptions.RemoveEmptyEntries);

                    if (lines.Length < 21)
                    {
                        Plugin.Warn($"Attempted to parse a malformed/invalid player token (expected 21 lines, got {lines.Length})!\n{token}");
                        return false;
                    }

                    tokenData = new TokenData();

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("User ID: "))
                            tokenData.UserId = line.Remove("User ID: ");
                        else if (line.StartsWith("Nickname: "))
                            tokenData.Nickname = line.Remove("Nickname: ");
                        else if (line.StartsWith("Request IP: "))
                            tokenData.Ip = line.Remove("Request IP: ");
                        else if (line.StartsWith("ASN: "))
                        {
                            var asnIdStr = line.Remove("ASN: ");

                            if (!int.TryParse(asnIdStr, out var asnId))
                            {
                                Plugin.Warn($"Failed to parse ASN ID string: {asnIdStr} ({line})");
                                continue;
                            }

                            tokenData.AsnId = asnId;
                        }
                        else if (line.StartsWith("Global ban: "))
                        {
                            var gBanStr = line.Remove("Global ban: ");

                            if (gBanStr != "NO")
                                Plugin.Warn($"Unknown global ban string: {gBanStr}");

                            tokenData.IsGloballyBanned = gBanStr != "NO";
                        }
                        else if (line.StartsWith("VAC session: "))
                        {
                            var vacStr = line.Remove("VAC session: ");
                            var isEmpty = string.IsNullOrWhiteSpace(vacStr);

                            if (!isEmpty && vacStr != "DISABLED")
                                Plugin.Warn($"Unknown VAC session string: {vacStr}");

                            tokenData.IsVacSession = vacStr != "DISABLED" && !isEmpty;
                        }
                        else if (line.StartsWith("Issuance time: "))
                        {
                            var issTimeStr = line.Remove("Issuance time: ");

                            if (!DateTime.TryParse(issTimeStr, out var issuanceTime))
                            {
                                Plugin.Warn($"Failed to parse issuance time from string: {issTimeStr} ({line})");
                                continue;
                            }

                            tokenData.IssuedAt = issuanceTime;
                        }
                        else if (line.StartsWith("Expiration time: "))
                        {
                            var expTimeStr = line.Remove("Expiration time: ");

                            if (!DateTime.TryParse(expTimeStr, out var expieryTime))
                            {
                                Plugin.Warn($"Failed to parse expiery time from string: {expTimeStr} ({line})");
                                continue;
                            }

                            tokenData.ExpiresAt = expieryTime;
                        }
                        else if (line.StartsWith("Issued by: "))
                            tokenData.ServerId = line.Remove("Issued by: ");
                        else if (line.StartsWith("Usage: "))
                            tokenData.UsageType = line.Remove("Usage: ");
                        else if (line.StartsWith("Skip IP Check: "))
                        {
                            var sIpCStr = line.Remove("Skip IP Check: ");

                            if (sIpCStr != "YES" && sIpCStr != "NO")
                                Plugin.Warn($"Unknown Skip IP Check value: {sIpCStr}");

                            tokenData.ShouldSkipIpCheck = sIpCStr == "YES";
                        }
                        else if (line.StartsWith("Bypass bans: "))
                        {
                            var bbStr = line.Remove("Bypass bans: ");

                            if (bbStr != "YES" && bbStr != "NO")
                                Plugin.Warn($"Unknown Bypass bans value: {bbStr}");

                            tokenData.CanBypassBans = bbStr == "YES";
                        }
                        else if (line.StartsWith("Bypass geo restrictions: "))
                        {
                            var bgStr = line.Remove("Bypass geo restrictions: ");

                            if (bgStr != "YES" && bgStr != "NO")
                                Plugin.Warn($"Unknown Bypass GEO value: {bgStr}");

                            tokenData.CanBypassGeoRestrictions = bgStr == "YES";
                        }
                        else if (line.StartsWith("Bypass WL: "))
                        {
                            var bwhStr = line.Remove("Bypass WL: ");

                            if (bwhStr != "YES" && bwhStr != "NO")
                                Plugin.Warn($"Unknown Bypass WL value: {bwhStr}");

                            tokenData.CanBypassWhitelist = bwhStr == "YES";
                        }
                        else if (line.StartsWith("Global badge: "))
                        {
                            var gbStr = line.Remove("Global badge: ");

                            if (gbStr != "NO")
                                Plugin.Warn($"Unknown Global badge value: {gbStr}");

                            tokenData.HasGlobalBadge = gbStr != "NO";
                        }
                        else if (line.StartsWith("EHID: "))
                            tokenData.EhId = line.Remove("EHID: ");
                        else if (line.StartsWith("Serial: "))
                            tokenData.SerialNumber = line.Remove("Serial: ");
                        else if (line.StartsWith("Public key: "))
                            tokenData.PublicPart = line.Remove("Public key: ");
                        else if (line.StartsWith("Test signature: "))
                        {
                            var tsStr = line.Remove("Test signature: ");

                            if (tsStr != "YES" && tsStr != "NO")
                                Plugin.Warn($"Unknown Test signature value: {tsStr} ({line})");

                            tokenData.IsTestSignature = tsStr == "YES";
                        }
                        else if (line.StartsWith("Auth Version: "))
                        {
                            var avStr = line.Remove("Auth Version: ");

                            if (!int.TryParse(avStr, out var authVersion))
                            {
                                Plugin.Warn($"Failed to parse Auth version: {avStr} ({line})");
                                continue;
                            }

                            tokenData.AuthVersion = authVersion;
                        }
                        else if (line.StartsWith("Signature: "))
                            tokenData.Signature = line.Remove("Signature: ");
                        else if (line.StartsWith("Do Not Track: "))
                        {
                            var dntStr = line.Remove("Do Not Track: ");

                            if (dntStr != "YES" && dntStr != "NO")
                                Plugin.Warn($"Unknown Do Not Track value: {dntStr} ({line})");

                            tokenData.IsDoNotTrack = dntStr == "YES";
                        }
                        else if (line.StartsWith("Sync Hashed: "))
                        {
                            var snhStr = line.Remove("Sync Hashed: ");

                            if (snhStr != "YES" && snhStr != "NO")
                                Plugin.Warn($"Unknown Sync Hashed value: {snhStr} ({line})");

                            tokenData.ShouldSyncHashed = snhStr == "YES";
                        }
                        else
                        {
                            Plugin.Warn($"Unidentified line encountered: \"{line}\"");
                            continue;
                        }
                    }
                }
                else
                {
                    tokenData = JsonHelper.FromJson<TokenData>(token);
                }
            }
            catch (Exception ex)
            {
                Plugin.Error($"Caught an exception while parsing player token! Token:\n{token}\nError: {ex.Message}\nException:\n{ex}");
                return false;
            }

            return tokenData != null;
        }
    }
}
