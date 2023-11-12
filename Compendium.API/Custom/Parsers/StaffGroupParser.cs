using BetterCommands.Parsing;

using Compendium.Staff;

using helpers;
using helpers.Results;

using System;

namespace Compendium.Custom.Parsers
{
    public class StaffGroupParser : ICommandArgumentParser
    {
        internal static void Load()
        {
            CommandArgumentParser.AddParser<StaffGroupParser>(typeof(StaffGroup));
            ArgumentUtils.SetFriendlyName(typeof(StaffGroup), "a staff group's key");
        }

        public IResult Parse(string value, Type type)
        {
            if (StaffHandler.Groups.TryGetFirst(g => string.Equals(value, g.Value.Key, StringComparison.OrdinalIgnoreCase), out var group)
                && group.Value != null)
                return Result.Success(group.Value);

            return Result.Error("No matching groups were found.");
        }
    }
}