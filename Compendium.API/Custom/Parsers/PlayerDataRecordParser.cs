using BetterCommands.Parsing;

using Compendium.PlayerData;

using helpers.Results;

using System;

namespace Compendium.Custom.Parsers
{
    public class PlayerDataRecordParser : ICommandArgumentParser
    {
        internal static void Load()
        {
            CommandArgumentParser.AddParser<PlayerDataRecordParser>(typeof(PlayerDataRecord));
            ArgumentUtils.SetFriendlyName(typeof(PlayerDataRecord), "a player's name, IP, user ID (for offline players) or player ID (for online players only).");
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