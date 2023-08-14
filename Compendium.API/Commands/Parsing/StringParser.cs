using Compendium.Commands.Parameters;

using helpers.Pooling.Pools;
using helpers.Results;

namespace Compendium.Commands.Parsing
{
    public static class StringParser
    {
        public static IResult TryParse(string line, bool ignoreExtra, int startPos, Parameter[] parameters)
        {
            Parameter curParam = null;

            var curPart = StringParserPart.None;

            var endPos = line.Length;
            var lastArgEndPos = int.MinValue;

            var argBuilder = StringBuilderPool.Pool.Get();
            var argList = ListPool<IResult>.Pool.Get();
            var paramList = ListPool<IResult>.Pool.Get();
            var isEscaping = false;

            char c, matchQuote = '\0';

            for (int curPos = startPos; curPos <= endPos; curPos++)
            {
                if (curPos < endPos)
                    c = line[curPos];
                else
                    c = '\0';

                if (curParam != null && curParam.IsRemainder() && curPos != endPos)
                {
                    argBuilder.Append(c);
                    continue;
                }

                if (isEscaping)
                {
                    if (curPos != endPos)
                    {
                        if (c != matchQuote)
                        {
                            argBuilder.Append('\\');
                        }

                        argBuilder.Append(c);
                        isEscaping = false;

                        continue;
                    }
                }

                if (c == '\\' && (curParam == null || !curParam.IsRemainder()))
                {
                    isEscaping = true;
                    continue;
                }

                if (curPart == StringParserPart.None)
                {
                    if (char.IsWhiteSpace(c) || curPos == endPos)
                        continue;
                    else if (curPos == lastArgEndPos)
                        return Result.Error("There must be at least one character of whitespace between arguments.");
                    else
                    {
                        if (curParam == null)
                            curParam = parameters.Length > argList.Count ? parameters[argList.Count] : null;

                        if (curParam != null && curParam.IsRemainder())
                        {
                            argBuilder.Append(c);
                            continue;
                        }

                        if (StringParserSettings.IsOpenQuote(c))
                        {
                            curPart = StringParserPart.QuotedParameter;
                            matchQuote = StringParserSettings.GetMatchingQuote(c);

                            continue;
                        }

                        curPart = StringParserPart.Parameter;
                    }
                }

                string argString = null;

                if (curPart == StringParserPart.Parameter)
                {
                    if (curPos == endPos || char.IsWhiteSpace(c))
                    {
                        argString = argBuilder.ToString();
                        lastArgEndPos = curPos;
                    }
                    else
                        argBuilder.Append(c);
                }
                else if (curPart == StringParserPart.QuotedParameter)
                {
                    if (c == matchQuote)
                    {
                        argString = argBuilder.ToString(); 
                        lastArgEndPos = curPos + 1;
                    }
                    else
                        argBuilder.Append(c);
                }

                if (argString != null)
                {
                    if (curParam == null)
                    {
                        if (ignoreExtra)
                            break;
                        else
                            return Result.Error("The input text has too many parameters.");
                    }

                    var parseResult = curParam.TryParse(argString);

                    if (!parseResult.IsSuccess)
                        return parseResult.CopyError();

                    if (curParam.IsMultiple())
                    {
                        paramList.Add(parseResult);
                        curPart = StringParserPart.None;
                    }
                    else
                    {
                        argList.Add(parseResult);

                        curParam = null;
                        curPart = StringParserPart.None;
                    }

                    argBuilder.Clear();
                }
            }

            if (curParam != null && curParam.IsRemainder())
            {
                var parseResult = curParam.TryParse(argBuilder.ToString());

                if (!parseResult.IsSuccess)
                    return parseResult.CopyError();

                argList.Add(parseResult);
            }

            if (isEscaping)
                return Result.Error("Input text may not end on an incomplete escape.");

            if (curPart == StringParserPart.QuotedParameter)
                return Result.Error("A quoted parameter is incomplete.");

            for (int i = argList.Count; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (param.IsMultiple())
                    continue;

                if (!param.IsOptional())
                    return Result.Error("The input text has too few parameters.");

                argList.Add(Result.Success(param.DefaultValue));
            }

            var args = argList.ToArray();
            var paramArray = paramList.ToArray();

            ListPool<IResult>.Pool.Push(argList);
            ListPool<IResult>.Pool.Push(paramList);

            StringBuilderPool.Pool.Push(argBuilder);

            return Result.Success((args, paramArray));
        }
    }
}