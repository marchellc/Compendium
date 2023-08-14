using Compendium.Commands.Parameters;

using helpers.Pooling.Pools;
using helpers.Results;

using System.Linq;

namespace Compendium.Commands.Parsing
{
    public static class ParsingUtils
    {
        public static float CalculateParsingScore(ICommand command, IResult parseResult)
        {
            var argValuesScore = 0f; 
            var paramValuesScore = 0f;

            if (!parseResult.TryReadResult<(IResult[] argList, IResult[] paramList)>(true, out var result))
                return 0f;

            var argValues = GetResults(result.argList);
            var paramValues = GetResults(result.paramList);

            if (command.Parameters.Length > 0)
            {            
                var argValuesSum = argValues?.Sum(x => x.Score) ?? 0;
                var paramValuesSum = paramValues?.Sum(x => x.Score) ?? 0;

                argValuesScore = argValuesSum / command.Parameters.Length;
                paramValuesScore = paramValuesSum / command.Parameters.Length;
            }

            var totalArgsScore = (argValuesScore + paramValuesScore) / 2;
            return (byte)command.Priority + totalArgsScore * 0.99f;
        }

        private static ParameterParserResult[] GetResults(IResult[] results)
        {
            var parserResults = ListPool<ParameterParserResult>.Pool.Get();

            foreach (var result in results)
            {
                if (result.TryReadResult<ParameterParserResult>(true, out var res))
                    parserResults.Add(res);
            }

            var array = parserResults.ToArray();
            ListPool<ParameterParserResult>.Pool.Push(parserResults);
            return array;
        }
    }
}