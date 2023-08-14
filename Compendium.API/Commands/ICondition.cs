using helpers.Results;

namespace Compendium.Commands
{
    public interface ICondition
    {
        IResult<object> Check(ReferenceHub sender);
    }
}