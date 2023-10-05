using helpers.Extensions;
using helpers.Values;

using Compendium.Enums;

using System;

namespace Compendium
{
    public struct UserIdValue : IValue<string>
    {
        private string _value;

        public const int DiscordIdLength = 18;
        public const int SteamIdLength = 17;

        public string Value
        {
            get => _value;
            set
            {
                if (!value.TrySplit('@', true, 2, out var split))
                {
                    if (!long.TryParse(value, out var numId))
                        throw new Exception();

                    if (!TryGetType(value.Length, out var type))
                        throw new Exception();

                    ClearId = value;
                    Id = numId;
                    Type = type;
                    TypeRepresentation = type.ToString().ToLower();
                    _value = $"{value}@{TypeRepresentation}";
                }
                else
                {

                    var idValue = split[0];
                    var idType = split[1];

                    if (!long.TryParse(idValue, out var parsedId))
                        throw new Exception();

                    if (!TryGetType(idType, out var parsedType)
                        && !TryGetType(idValue.Length, out parsedType))
                        throw new Exception();

                    ClearId = idValue;
                    TypeRepresentation = parsedType.ToString().ToLower();
                    Id = parsedId;
                    Type = parsedType;
                    _value = value;
                }
            }
        }

        public string ClearId { get; private set; }
        public string TypeRepresentation { get; private set; }

        public long Id { get; private set; }

        public UserIdType Type { get; private set; }

        public UserIdValue(string id)
            => Value = id;

        public static bool TryParse(string id, out UserIdValue value)
        {
            try
            {
                value = new UserIdValue(id);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        private static bool TryGetType(int length, out UserIdType userIdType)
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

        private static bool TryGetType(string idType, out UserIdType type)
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
                    type = UserIdType.Unknown;
                    return false;
            }
        }
    }
}