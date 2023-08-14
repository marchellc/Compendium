using Compendium.Commands.Parameters;

using helpers;
using helpers.Results;

using System.Collections.Generic;
using System.Reflection;

namespace Compendium.Commands
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        string Usage { get; }

        bool IgnoreArgs { get; }

        object Handle { get; }

        ICommandGroup Parent { get; }
        ICondition[] Conditions { get; }

        Priority Priority { get; }

        MethodInfo Target { get; }

        Parameter[] Parameters { get; }

        IResult Parse(string line, int pos);
        IResult Invoke(ICommandContext context, Queue<ParameterParserResult> parserResults);

        void SetParent(ICommandGroup group);
    }
}