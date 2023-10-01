using Compendium.UserId;

namespace Compendium.Comparison
{
    public static class UserIdComparison
    {
        public static bool Compare(string uid, string uid2)
        {
            if (!UserIdHelper.TryParse(uid, out var userId))
                return false;

            if (!UserIdHelper.TryParse(uid2, out var userId2))
                return false;

            return userId.TryMatch(userId2);
        }
    }
}