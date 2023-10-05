namespace Compendium.Constants
{
    public static class Colors
    {
        public const string LightGreenValue = "#33FFA5";
        public const string RedValue = "#FF0000";
        public const string GreenValue = "#90FF33";

        public static string LightGreen(string str)
            => $"<color={LightGreenValue}>{str}</color>";

        public static string Red(string str)
            => $"<color={RedValue}>{str}</color>";

        public static string Green(string str)
            => $"<color={GreenValue}>{str}</color>";
    }
}