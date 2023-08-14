using Compendium.Commands.Parameters;
using Compendium.Commands.Parsing;

using helpers;
using helpers.Enums;
using helpers.Results;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Compendium.Commands
{
    public class Command : ICommand
    {
        private ICommandGroup _parent;

        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }

        public bool IgnoreArgs { get; }

        public object Handle { get; }

        public ICommandGroup Parent => _parent;
        public ICondition[] Conditions { get; }

        public Priority Priority { get; }

        public MethodInfo Target { get; }

        public Parameter[] Parameters { get; }

        public Command(string name, string description, string usage, bool ignoreArgs, object handle, ICondition[] conditions, Priority priority, MethodInfo target, Parameter[] parameters)
        {
            Name = name;
            Description = description;
            Usage = usage;

            IgnoreArgs = ignoreArgs;

            Handle = handle;

            Conditions = conditions;

            Priority = priority;

            Target = target;

            Parameters = parameters;
        }

        public IResult Invoke(ICommandContext context, Queue<ParameterParserResult> parserResults)
        {
            var objArray = new object[Parameters.Length];

            for (int i = 0; i < Parameters.Length; i++)
            {
                if (Parameters[i].Flags.HasFlagFast(ParameterFlags.Context))
                {
                    objArray[i] = context;
                    continue;
                }

                if (Parameters[i].Flags.HasFlagFast(ParameterFlags.SenderHub))
                {
                    objArray[i] = context.Hub;
                    continue;
                }

                if (Parameters[i].Flags.HasFlagFast(ParameterFlags.Sender))
                {
                    objArray[i] = context.Player;
                    continue;
                }

                objArray[i] = parserResults.Dequeue();
            }

            object result = null;

            try
            {
                result = Target.Invoke(Handle, objArray);
            }
            catch (Exception ex)
            {
                return Result.Error(null, ex);
            }

            return Result.Success(result);
        }

        public IResult Parse(string line, int pos)
            => StringParser.TryParse(line, IgnoreArgs, pos, Parameters);

        public void SetParent(ICommandGroup group)
        {
            if (_parent != null)
                _parent.Remove(this);

            _parent = group;
            _parent.Add(this);
        }
    }
}
