using System;

namespace Compendium.Commands.Parameters
{
    public interface IParameterRestriction
    {
        bool IsValid(Type type);

        bool Check(object value);
    }
}