using helpers.Results;
using System;

namespace Compendium.Commands.Parameters
{
    public interface IParameterParser
    {
        bool TryValidate(ParameterType type);

        IResult TryParse(string value, Type originalType);
    }
}