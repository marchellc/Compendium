using helpers.Extensions;
using helpers.Results;

using System;

namespace Compendium.Commands.Parameters
{
    public class Parameter
    {
        public string Name { get; }

        public int Index { get; }

        public object DefaultValue { get; }

        public Type Type { get; }

        public ParameterType TypeId { get; }
        public ParameterFlags Flags { get; }

        public IParameterRestriction[] Restrictions { get; }
        public IParameterParser Parser { get; }

        public Parameter(string name, int index, object defValue, Type type, ParameterType typeId, ParameterFlags flags, IParameterRestriction[] restrictions, IParameterParser parser)
        {
            Name = name;
            Index = index;
            DefaultValue = defValue;
            Type = type;
            TypeId = typeId;
            Flags = flags;
            Restrictions = restrictions;
            Parser = parser;
        }

        public IResult TryParse(string value)
        {
            if (Parser is null)
                return Result.Error($"Missing parser for parameter \"{Name}\" ({Index})");

            var parseResult = Parser.TryParse(value, Type);

            if (!parseResult.TryReadResult<ParameterParserResult>(true, out var parserResult))
                return Result.Error($"Parsing failed: \"{Name}\" ({Index})");

            if (Restrictions.Any())
            {
                foreach (var restriction in Restrictions)
                {
                    if (!restriction.Check(parserResult.Value))
                    {
                        return Result.Error($"Restriction check failed: \"{Name}\" ({Index})");
                    }
                }
            }

            return Result.Success(parserResult);
        }
    }
}