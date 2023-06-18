namespace Compendium.Helpers.Hints
{
    public static class HintUtils
    {
        public static string PercentageToString(double percentage) => $"{percentage}%";
        public static string PixelsToString(double pixels) => $"{pixels}px";
        public static string UnitsToString(double units) => $"{units}em";

        public static string ConvertTag(HintTag tag)
        {
            switch (tag)
            {
                case HintTag.Bold:
                    return "b";

                case HintTag.FontWeight:
                    return "font-weight";

                case HintTag.Italics:
                    return "i";

                case HintTag.LineHeight:
                    return "line-height";

                case HintTag.LineIndent:
                    return "line-indent";

                case HintTag.Position:
                    return "pos";

                case HintTag.Strikethrough:
                    return "s";

                case HintTag.Underlined:
                    return "u";

                case HintTag.MarginLeft:
                    return "margin-left";

                case HintTag.MarginRight:
                    return "margin-right";

                default:
                    return tag.ToString().ToLower();
            }
        }
    }
}