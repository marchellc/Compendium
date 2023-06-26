using UnityEngine;

namespace Compendium.Extensions.RichText
{
    public static class RichTextExtensions
    {
        public static string WrapWithTag(this string text, string tag) => $"<{tag}>{text}</{tag}>";
        public static string WrapWithTag(this string text, string tag, string value)
            => value is null ? text.WrapWithTag(tag) : $"<{tag}={value}>{text}</{tag}>";

        public static string Bold(this string text) => text.WrapWithTag("b");

        public static string Italic(this string text) => text.WrapWithTag("i");

        public static string Underline(this string text) => text.WrapWithTag("u");

        public static string Strikethrough(this string text) => text.WrapWithTag("s");

        public static string Superscript(this string text) => text.WrapWithTag("sup");

        public static string Subscript(this string text) => text.WrapWithTag("sub");

        public static string Color(this string text, string color) => text.WrapWithTag("color", color);
        public static string Color(this string text, Color color, bool alpha = false) => text.Color(color.ToHex(alpha));

        public static string Size(this string text, int size) => text.Size($"{size}px");
        public static string Size(this string text, string size) => text.WrapWithTag("size", size);

        public static string Align(this string text, RichTextAlignment alignment) => text.WrapWithTag(alignment.ToString().ToLower());

        public static string Mark(this string text, string color) => text.WrapWithTag("mark", color);
        public static string Mark(this string text, Color color) => text.Mark(color.ToHex());
        public static string Mark(this string text, Color color, byte alpha) => text.Mark(color.ToHex(true, false) + alpha.ToString("X2"));

        public static string NoParse(this string text) => text.WrapWithTag("noparse");

        public static string Capitalize(this string text, RichTextCapitalization mode) => text.WrapWithTag(mode.ToString().ToLower());

        public static string CharacterSpacing(this string text, int spacing) => text.CharacterSpacing($"{spacing}px");
        public static string CharacterSpacing(this string text, string spacing) => text.WrapWithTag("cspace", spacing);

        public static string Indent(this string text, int amount) => text.Indent($"{amount}px");
        public static string Indent(this string text, string amount) => text.WrapWithTag("indent", amount);

        public static string LineHeight(this string text, int spacing) => text.LineHeight($"{spacing}px");
        public static string LineHeight(this string text, string spacing) => $"<line-height={spacing}>{text}";

        public static string LineIndent(this string text, int amount) => text.LineIndent($"{amount}px");
        public static string LineIndent(this string text, string amount) => $"<line-indent={amount}>{text}";

        public static string Link(this string text, string id) => text.WrapWithTag("link", $"\"{id}\"");

        public static string HorizontalPosition(this string text, int offset) => text.HorizontalPosition($"{offset}px");
        public static string HorizontalPosition(this string text, string offset) => $"<pos={offset}>{text}";

        public static string Margin(this string text, int margin, RichTextAlignment alignment = RichTextAlignment.Center) => text.Margin($"{margin}px", alignment);
        public static string Margin(this string text, string margin, RichTextAlignment alignment = RichTextAlignment.Center)
            => $"<margin{(alignment == RichTextAlignment.Center ? "" : $"-{alignment.ToString().ToLower()}")}>{text}";

        public static string Monospace(this string text, float spacing = 1f) => text.WrapWithTag("mspace", $"{spacing}em");
        public static string Monospace(this string text, string spacing) => text.WrapWithTag("mspace", spacing);

        public static string VerticalOffset(this string text, int offset) => text.VerticalOffset($"{offset}px");
        public static string VerticalOffset(this string text, string offset) => text.WrapWithTag("voffset", offset);

        public static string MaxWidth(this string text, int width) => text.MaxWidth($"{width}px");
        public static string MaxWidth(this string text, string width) => $"<width={width}>{text}";

        public static string Sprite(int index, string color = null) => $"<sprite={index}{(color is null ? "" : $" color={color}")}>";
        public static string Sprite(int index, Color color) => $"<sprite={index} color={color.ToHex()}>";

        public static string Space(int amount) => $"<space={amount}px>";
        public static string Space(string amount) => $"<space={amount}>";
    }
}