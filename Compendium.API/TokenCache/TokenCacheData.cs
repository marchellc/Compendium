using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.TokenCache
{
    public class TokenCacheData
    {
        public string Signature { get; set; } = "default";
        public string Public { get; set; } = "default";
        public string EhId { get; set; } = "default";
        public string UniqueId { get; set; } = "default";

        public List<string> AllSerials { get; set; } = new List<string>();

        public Dictionary<DateTime, string> Ips { get; set; } = new Dictionary<DateTime, string>();
        public Dictionary<DateTime, string> Ids { get; set; } = new Dictionary<DateTime, string>();
        public Dictionary<DateTime, string> Nicknames { get; set; } = new Dictionary<DateTime, string>();
        public Dictionary<DateTime, DateTime> Sessions { get; set; } = new Dictionary<DateTime, DateTime>();

        public string LastSerial => AllSerials.Last();
        public string LastId => Ids.Last().Value;
        public string LastIp => Ips.Last().Value;
        public string LastNickname => Nicknames.Last().Value;

        public DateTime LastNicknameChange => Nicknames.Last().Key;
        public DateTime LastIdChange => Ids.Last().Key;
        public DateTime LastIpChange => Ips.Last().Key;

        public DateTime LastJoin => Sessions.Last().Key;
        public DateTime LastLeave => Sessions.Last().Value;

        public TimeSpan TotalPlaytime
        {
            get
            {
                // key - join
                // value - leave

                RecordSessionEnd();

                var totalSeconds = 0;

                foreach (var session in Sessions)
                {
                    var value = session.Value;

                    totalSeconds += (int)Math.Ceiling((value - session.Key).TotalSeconds);
                }

                return TimeSpan.FromSeconds(totalSeconds);
            }
        }

        public TimeSpan TwoWeeksPlaytime
        {
            get
            {
                var minDay = DateTime.Now.Day - 13;
                var totalSeconds = 0;

                RecordSessionEnd();

                foreach (var session in Sessions)
                {
                    if (session.Key.Day < minDay || session.Value.Day < minDay)
                        continue;

                    totalSeconds += (int)Math.Ceiling((session.Value - session.Key).TotalSeconds);
                }

                return TimeSpan.FromSeconds(totalSeconds);
            }
        }

        public TimeSpan PlaytimeBetween(DateTime min, DateTime max)
        {
            // key - join
            // value - leave

            RecordSessionEnd();

            var totalSeconds = 0;

            foreach (var session in Sessions)
            {
                var value = session.Value;

                if (session.Key < min || session.Key > max)
                    continue;

                if (value < min || value > max)
                    continue;

                totalSeconds += (int)Math.Ceiling((value - session.Key).TotalSeconds);
            }

            return TimeSpan.FromSeconds(totalSeconds);
        }

        public void RecordNicknameChange(string curNick)
            => Nicknames.Add(DateTime.Now.ToLocalTime(), curNick);

        public void RecordIdChange(string id)
            => Ids.Add(DateTime.Now.ToLocalTime(), id);

        public void RecordIpChange(string ip)
            => Ips.Add(DateTime.Now.ToLocalTime(), ip);

        public bool CompareNick(string curNick)
        {
            if (!Nicknames.Any() || LastNickname != curNick)
            {
                RecordNicknameChange(curNick);
                return true;
            }

            return false;
        }

        public bool CompareId(string curId)
        {
            if (!Ids.Any() || LastId != curId)
            {
                RecordIdChange(curId);
                return true;
            }

            return false;
        }

        public bool CompareIp(string curIp)
        {
            if (!LastIp.Any() || LastIp != curIp)
            {
                RecordIpChange(curIp);
                return true;
            }

            return false;
        }

        public bool RecordSerial(string serial)
        {
            if (!AllSerials.Any() || LastSerial != serial)
            {
                AllSerials.Add(serial);
                return true;
            }

            return false;
        }

        public void RecordSessionStart()
        {
            if (Sessions.Any())
            {
                if (Sessions.Last().Value <= DateTime.MinValue)
                    RecordSessionEnd();
            }

            Sessions[DateTime.Now.ToLocalTime()] = DateTime.MinValue;
        }

        public void RecordSessionEnd()
            => Sessions[Sessions.Last().Key] = DateTime.Now.ToLocalTime();
    }
}