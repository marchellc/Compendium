using System;

namespace Compendium.Comparison
{
    public static class NullableObjectComparison
    {
        public static bool Compare(object obj1, object obj2)
        {
            if (obj1 is null && obj2 is null)
                return true;

            if ((obj1 != null && obj2 is null) || (obj1 is null && obj2 != null))
                return false;

            try
            {
                return obj1.Equals(obj2) || obj1.GetHashCode() == obj2.GetHashCode();
            }
            catch (Exception ex)
            {
                Plugin.Error($"Object Comparison failed: {ex}");
                return false;
            }
        }
    }
}