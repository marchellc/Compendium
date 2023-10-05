namespace Compendium.Comparison
{
    public static class UserIdComparison
    {
        public static bool Compare(string uid, string uid2)
        {
            if (!UserIdValue.TryParse(uid, out var userId))
                return false;

            if (!UserIdValue.TryParse(uid2, out var userId2))
                return false;

            return userId.Value == userId2.Value;
        }
    }
}