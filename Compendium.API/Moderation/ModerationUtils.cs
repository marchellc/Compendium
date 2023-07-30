using Compendium.Rules;

using helpers.Extensions;
using helpers.Time;

using System;

namespace Compendium.Moderation
{
    public static class ModerationUtils
    {
        public static bool TryParseDuration(string str, out (RuleData[] rules, TimeSpan? added) duration)
        {
            duration.rules = null;
            duration.added = null;

            if (str.TryParse(out var parts))
            {
                if (!RuleSystem.TryParseRules(parts[0], out var rules))
                    return false;

                duration.rules = rules;

                if (parts.Length > 1 && TimeUtils.TryParseTime(parts[1], out var added))
                    duration.added = added;
            }

            return duration.rules != null && (duration.rules.Any() || duration.added.HasValue);
        }
    }
}