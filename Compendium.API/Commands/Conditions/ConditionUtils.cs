using Compendium.Commands.Attributes;

using helpers.Extensions;
using helpers.Results;

using System.Collections.Generic;
using System.Reflection;

namespace Compendium.Commands.Conditions
{
    public class ConditionUtils
    {
        public static ICondition[] CollectConditions(MethodInfo method)
        {
            var list = new List<ICondition>();

            method.GetCustomAttributes<ConditionAttribute>().ForEach(condition =>
            {
                if (condition.Condition != null)
                    list.Add(condition.Condition);
            });

            return list.ToArray();
        }

        public static IResult CheckConditions(ReferenceHub sender, ICondition[] conditions)
        {
            if (conditions.IsEmpty())
                return Result.Success();

            foreach (var condition in conditions)
            {
                var result = condition.Check(sender);

                if (!result.IsSuccess)
                    return result.CopyError();
            }

            return Result.Success();
        }
    }
}