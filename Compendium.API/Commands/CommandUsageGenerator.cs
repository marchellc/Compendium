using Compendium.Commands.Parameters;

namespace Compendium.Commands
{
    public static class CommandUsageGenerator
    {
        public static bool TryGenerateUsage(Parameter[] parameters, out string usage)
        {
            usage = "Test Usage";
            return true;
        }
    }
}
