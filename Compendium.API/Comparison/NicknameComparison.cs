using helpers.Extensions;

namespace Compendium.Comparison
{
    public static class NicknameComparison
    {
        public static bool Compare(string nick1, string nick2, double minScore = 0.8)
            => nick1.IsSimilar(nick2, minScore);
    }
}