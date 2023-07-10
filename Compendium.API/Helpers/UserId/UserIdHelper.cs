using helpers.Extensions;

using PluginAPI.Core;

namespace Compendium.Helpers.UserId
{
    public static class UserIdHelper
    {
        public const int DiscordIdLength = 18;
        public const int SteamIdLength = 17;

        public static bool TryParse(string id, out UserIdValue userId)
        {
            userId = default;

            if (!id.TrySplit('@', true, 2, out var split))
            {
                if (!long.TryParse(id, out var numId))
                    return false;

                if (!TryGetType(id.Length, out var type))
                    return false;

                var typeStr = $"{type.ToString().ToLower()}";
                var fullId = $"{id}@{typeStr}";

                userId = new UserIdValue(fullId, id, typeStr, numId, type);
                return true;
            }
            else
            {

                var idValue = split[0];
                var idType = split[1];

                if (!long.TryParse(idValue, out var parsedId))
                    return false;

                if (!TryGetType(idType, out var parsedType) && !TryGetType(idValue.Length, out parsedType))
                    return false;

                userId = new UserIdValue(id, idValue, idType, parsedId, parsedType);
                return true;
            }
        }

        public static bool TryGetType(int length, out UserIdType userIdType)
        {
            if (length == DiscordIdLength)
            {
                userIdType = UserIdType.Discord;
                return true;
            }    
            else if (length == SteamIdLength)
            {
                userIdType = UserIdType.Steam;
                return true;
            }
            else
            {
                userIdType = UserIdType.Unknown;
                return false;
            }
        }

        public static bool TryGetType(string idType, out UserIdType type)
        {
            idType = idType.ToLower();

            switch (idType)
            {
                case "northwood":
                    type = UserIdType.Northwood;
                    return true;

                case "patreon":
                    type = UserIdType.Patreon;
                    return true;

                case "steam":
                    type = UserIdType.Steam;
                    return true;

                case "discord":
                    type = UserIdType.Discord;
                    return true;

                default:
                    Log.Warning($"Unknown UID type: {idType}", "UserID Helper");
                    type = UserIdType.Unknown;
                    return false;
            }
        }
    }
}
