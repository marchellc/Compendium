using BetterCommands.Parsing;

using Compendium.PlayerData;

using helpers.Attributes;
using helpers.Results;

using System;

namespace Compendium.Parsers
{
    public class PlayerDataRecordParser : ICommandArgumentParser
    {
        internal static void Load()
        {
            CommandArgumentParser.AddParser<PlayerDataRecordParser>(typeof(PlayerDataRecord));
            Plugin.Debug($"PlayerDataRecordParser registered");
        }

        public IResult Parse(string value, Type type)
        {
            if (!PlayerDataRecorder.TryQuery(value, true, out var record))
                return Result.Error("Failed to find that data record.");
            else
                return Result.Success(record);
        }
    }
}