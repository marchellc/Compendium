using Compendium.Commands.Conditions;
using Compendium.Commands.Groups;
using Compendium.Commands.Parsing;
using Compendium.Commands.Parameters;
using Compendium.Commands.Responses;
using Compendium.Commands.Context;
using Compendium.Commands.Attributes;
using Compendium.Round;

using helpers;
using helpers.Pooling.Pools;
using helpers.Results;
using helpers.Extensions;

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using System.Reflection;

namespace Compendium.Commands
{
    public static class CommandHandler
    {
        private static readonly Dictionary<ReferenceHub, ContinuedResponseBase> _continuedResponses;

        private static readonly ICommandGroup _raGroup;
        private static readonly ICommandGroup _plyGroup;
        private static readonly ICommandGroup _srvGroup;

        public static ICommandGroup RemoteAdminGroup => _raGroup;
        public static ICommandGroup PlayerConsoleGroup => _plyGroup;
        public static ICommandGroup ServerConsoleGroup => _srvGroup;

        public static IReadOnlyDictionary<ReferenceHub, ContinuedResponseBase> WaitingResponses => _continuedResponses;

        public static bool IsCaseSensitive => Plugin.Config != null && Plugin.Config.CommandSettings != null && Plugin.Config.CommandSettings.IsCaseSensitive;

        public static int TopPartCount { get; internal set; } = 1;

        static CommandHandler()
        {
            _raGroup = new SourceCommandGroup(CommandSource.RemoteAdmin);
            _plyGroup = new SourceCommandGroup(CommandSource.PlayerConsole);
            _srvGroup = new SourceCommandGroup(CommandSource.ServerConsole);
            _continuedResponses = new Dictionary<ReferenceHub, ContinuedResponseBase>();
        }

        public static void RemoveCommands(Assembly assembly)
        {
            _raGroup.Commands.Where(c => c.Target.DeclaringType.Assembly == assembly).ForEach(_raGroup.Remove);
            _srvGroup.Commands.Where(c => c.Target.DeclaringType.Assembly == assembly).ForEach(_srvGroup.Remove);
            _plyGroup.Commands.Where(c => c.Target.DeclaringType.Assembly == assembly).ForEach(_plyGroup.Remove);
        }

        public static void RegisterCommands(Assembly assembly, ICommandGroup group = null)
            => assembly.ForEachType(type => RegisterCommands(type, null, group));

        public static void RegisterCommands(Type type, object typeInstance, ICommandGroup group = null)
            => type.ForEachMethod(method => RegisterCommand(method, typeInstance, group));

        public static void RegisterCommand(MethodInfo method, object typeInstance, ICommandGroup group = null)
        {
            if (method.TryGetAttribute<CommandAttributeBase>(out var cmdAttribute))
            {
                if (!cmdAttribute.Validate())
                {
                    Plugin.Warn($"Found an invalid command attribute on method {method.ToLogName(false)}");
                    return;
                }

                if (!ParameterUtils.TryConvertParameters(method.GetParameters(), out var parameters))
                {
                    Plugin.Warn($"Method {method.ToLogName(false)} contains invalid parameters!");
                    return;
                }

                var searchResult = Search(cmdAttribute.Name, cmdAttribute.Source);

                if (searchResult.TryReadResult<Tuple<ICommand[], int>>(true, out var tuple) && tuple.Item1.Any())
                {
                    foreach (var cmd in tuple.Item1)
                    {
                        if (cmd.Parameters.Select(x => x.Type).Match(parameters.Select(x => x.Type)))
                        {
                            Plugin.Warn($"Attempted to register command {cmd.Name} again!");
                            return;
                        }
                    }
                }

                var ignoreExtra = method.IsDefined(typeof(IgnoreExtraArgumentsAttribute));
                var usage = CommandUsageGenerator.TryGenerateUsage(parameters, out var usg) ? usg : "Unknown usage!";
                var aliases = method.TryGetAttribute<CommandAliasesAttribute>(out var aliasesAttribute) ? aliasesAttribute.Aliases : Array.Empty<string>();
                var name = cmdAttribute.Name;
                var desc = cmdAttribute.Description;
                var conditions = ConditionUtils.CollectConditions(method);
                var priority = method.TryGetAttribute<CommandPriorityAttribute>(out var commandPriorityAttribute) ? commandPriorityAttribute.Priority : Priority.Normal;
                var groupId = method.TryGetAttribute<CommandGroupAttribute>(out var commandGroupAttribute) ? commandGroupAttribute.Group : null;
                var command = new Command(name, desc, usage, ignoreExtra, typeInstance, conditions, priority, method, parameters);

                if (group is null)
                {
                    if (!string.IsNullOrWhiteSpace(groupId))
                    {
                        if (!TryGetGroup(groupId, out group))
                        {
                            Plugin.Warn($"Failed to find a group with ID {groupId}!");
                            return;
                        }
                    }
                    else
                    {
                        group = GetGroup(cmdAttribute.Source);
                    }
                }

                if (group is null)
                {
                    Plugin.Warn($"Command Group for source {cmdAttribute.Source} is null!");
                    return;
                }

                command.SetParent(group);

                Plugin.Debug($"Registered command: {command.Name}");
            }
        }

        public static void RegisterModule<TModule>(CommandSource source) where TModule : ICommandModule, new()
            => RegisterModule(new TModule(), source);

        public static void RegisterModule(ICommandModule module, CommandSource source)
        {
            var group = GetGroup(source);

            if (group is null)
            {
                Plugin.Warn($"Failed to register module {module.GetType().FullName}: source {source} has not been found.");
                return;
            }

            group.Add(module);

            RegisterCommands(module.GetType(), module);

            Plugin.Info($"Registered command module {module.GetType().FullName} in group {group.Name}");
        }

        public static bool TryExecuteContinued(ReferenceHub hub, string value, out string responseStr)
        {
            responseStr = null;

            if (_continuedResponses.TryGetValue(hub, out var response))
            {
                if (!ParameterUtils.TryFixType(response.ResponseType, out var parseType))
                    return false;

                if (!ParameterUtils.TryGetParameterType(parseType, out var paramType))
                    return false;

                if (!ParameterUtils.TryGetParser(paramType, out var parser))
                    return false;

                var parseResult = parser.TryParse(value, response.ResponseType);

                if (!parseResult.TryReadResult<ParameterParserResult>(true, out var result))
                    return false;

                var resp = Calls.Delegate<IResponse>(response.Callback, result.Value);

                if (resp is ContinuedResponseBase nextContinued)
                    _continuedResponses[hub] = nextContinued;
                else
                    _continuedResponses.Remove(hub);

                responseStr = resp.FormulateString();
                return true;
            }

            return false;
        }

        public static IResult TryExecute(CommandSource source, string cmd, string input, ReferenceHub sender)
        {
            var searchResult = Search(cmd, source);

            if (!searchResult.IsSuccess)
                return null;

            var ctx = new CommandContext(sender, source, input, cmd);
            var validationResult = Validate(searchResult, ctx);

            if (!validationResult.IsSuccess || !validationResult.TryReadResult<Tuple<ICommand, IEnumerable<IResult>>>(true, out var tuple))
                return null;

            var invokeResult = Invoke(validationResult, tuple.Item2.Select(r => r.ReadResult<ParameterParserResult>(true)).ToArray(), ctx);

            if (!invokeResult.IsSuccess)
                return invokeResult.CopyError();

            return invokeResult;
        }

        public static IResult Search(string input, CommandSource source)
        {
            input = IsCaseSensitive ? input : input.ToLowerInvariant();

            var group = GetGroup(source);

            if (group is null)
            {
                Plugin.Warn($"Failed to find command group for source: {source}");
                return Result.Error($"Failed to find command group for source: {source}");
            }

            var matches = group.QueryCommands(input, out var pos).OrderByDescending(c => (byte)c.Priority).ToArray();

            if (matches.Length > 0)
                return Result.Success(new Tuple<ICommand[], int>(matches, pos));
            else
                return Result.Error("Unknown command!");
        }

        public static IResult Invoke(IResult validationResult, ParameterParserResult[] parserResults, ICommandContext context)
        {
            if (!validationResult.TryReadResult<ICommand>(true, out var command))
                return validationResult.CopyError();

            var execResult = command.Invoke(context, parserResults.ToQueue());

            if (!execResult.IsSuccess)
                return execResult.CopyError();

            if (execResult.TryReadResult<string>(true, out var responseStr))
                return Result.Success(responseStr);

            if (execResult.TryReadResult<StringBuilder>(true, out var responseBuilder))
                return Result.Success(responseBuilder.ToString());

            if (execResult.TryReadResult<IEnumerable>(true, out var responseList))
                return Result.Success(string.Join("\n", responseList));

            if (execResult.TryReadResult<IResponse>(true, out var response))
            {
                if (response is ContinuedResponseBase continuedResponse)
                    _continuedResponses[context.Hub] = continuedResponse;

                return Result.Success(response.FormulateString());
            }

            return Result.Error($"Unknown response type received!");
        }

        public static IResult Validate(IResult searchResult, ICommandContext context)
        {
            if (!searchResult.TryReadResult < Tuple<ICommand[], int>>(true, out var tuple))
                return Result.Error();

            var commands = tuple.Item1;

            if (!commands.Any())
                return Result.Error();

            var conditions = DictionaryPool<ICommand, IResult>.Pool.Get();

            commands.ForEach(c =>
            {
                conditions[c] = ConditionUtils.CheckConditions(context.Hub, c.Conditions);
            });

            var succesfull = conditions.Where(c => c.Value.IsSuccess).ToArray();

            if (!succesfull.Any())
            {
                DictionaryPool<ICommand, IResult>.Pool.Push(conditions);
                return Result.Error();
            }

            var parsing = DictionaryPool<ICommand, IResult>.Pool.Get();

            succesfull.ForEach(p =>
            {
                parsing[p.Key] = p.Key.Parse(context.Query, tuple.Item2);
            });

            var weighted = parsing.OrderByDescending(p => ParsingUtils.CalculateParsingScore(p.Key, p.Value));
            var succesfullParsing = weighted.Where(p => p.Value.IsSuccess);

            if (!succesfullParsing.Any())
            {
                DictionaryPool<ICommand, IResult>.Pool.Push(parsing);
                DictionaryPool<ICommand, IResult>.Pool.Push(conditions);

                return Result.Error();
            }

            var chosen = succesfullParsing.First().Key;

            DictionaryPool<ICommand, IResult>.Pool.Push(parsing);
            DictionaryPool<ICommand, IResult>.Pool.Push(conditions);

            return Result.Success(new Tuple<ICommand, IEnumerable<IResult>>(chosen, succesfullParsing.Where(p => p.Key == chosen).Select(p => p.Value)));
        }

        private static ICommandGroup GetGroup(CommandSource source)
        {
            switch (source)
            {
                case CommandSource.PlayerConsole:
                    return _plyGroup;

                case CommandSource.ServerConsole:
                    return _srvGroup;

                case CommandSource.RemoteAdmin:
                    return _raGroup;

                default:
                    return null;
            }
        }

        [RoundStateChanged(RoundState.Ending)]
        private static void OnRoundEnd()
            => _continuedResponses.Clear();

        private static bool TryGetGroup(string groupId, out ICommandGroup commandGroup)
        {
            var parts = groupId.Split(' ');
            var topGroups = new ICommandGroup[] { _raGroup, _srvGroup, _plyGroup };

            foreach (var g in topGroups)
            {
                if (parts.Length != 1)
                {
                    var curGroup = g;

                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (curGroup.Children.TryGetFirst(gr => CommandGroup.CompareStrings(parts[i], gr.Name), out var group))
                        {
                            if (i >= parts.Length)
                            {
                                if (CommandGroup.CompareStrings(parts[i], curGroup.Name))
                                {
                                    commandGroup = curGroup;
                                    return true;
                                }
                                else
                                {
                                    commandGroup = null;
                                    return false;
                                }
                            }
                            else
                            {
                                curGroup = group;
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    if (g.Children.TryGetFirst(gr => CommandGroup.CompareStrings(groupId, gr.Name), out var group))
                    {
                        commandGroup = group;
                        return true;
                    }
                    else
                    {
                        commandGroup = null;
                        return false;
                    }
                }
            }

            commandGroup = null;
            return false;
        }
    }
}