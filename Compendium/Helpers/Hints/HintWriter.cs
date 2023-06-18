using System;

namespace Compendium.Helpers.Hints
{
    public class HintWriter
    {
        private string m_String;

        public HintWriter EmitAlign(HintAlign align) => EmitTag(HintTag.Align, align.Value);

        public HintWriter EmitColor(string color) => EmitTag(HintTag.Color, color);
        public HintWriter EmitMark(string color) => EmitTag(HintTag.Mark, color);
        public HintWriter EmitFont(string fontName) => EmitTag(HintTag.Font, fontName);
        public HintWriter EmitGradient(string gradientFunction) => EmitTag(HintTag.Gradient, gradientFunction);
        public HintWriter EmitLink(string link) => EmitTag(HintTag.Link, link);
        public HintWriter EmitAlpha(string alphaValue) => EmitTag(HintTag.Alpha, alphaValue);
        public HintWriter EmitSprite(string spriteName) => EmitTag(HintTag.Sprite, spriteName, "name");
        public HintWriter EmitStyle(string style) => EmitTag(HintTag.Style, style);

        public HintWriter EmitFontWeight(int weight) => EmitTag(HintTag.FontWeight, weight);
        public HintWriter EmitIndent(int indentPercentage) => EmitTag(HintTag.Indent, HintUtils.PercentageToString(indentPercentage));
        public HintWriter EmitLineHeight(int heightPercentage) => EmitTag(HintTag.LineHeight, HintUtils.PercentageToString(heightPercentage));
        public HintWriter EmitLineIndent(int lineIndentPercentage) => EmitTag(HintTag.LineIndent, HintUtils.PercentageToString(lineIndentPercentage));
        public HintWriter EmitPosition(int positionPercentage) => EmitTag(HintTag.Position, HintUtils.PercentageToString(positionPercentage));
        public HintWriter EmitSize(int sizePercentage) => EmitTag(HintTag.Size, HintUtils.PercentageToString(sizePercentage));
        public HintWriter EmitSprite(int spriteIndex) => EmitTag(HintTag.Sprite, spriteIndex);
        public HintWriter EmitWidth(int widthPercentage) => EmitTag(HintTag.Width, HintUtils.PercentageToString(widthPercentage));

        public HintWriter EmitCSpace(double space) => EmitTag(HintTag.CSpace, HintUtils.UnitsToString(space));
        public HintWriter EmitMargin(double margin) => EmitTag(HintTag.Margin, HintUtils.UnitsToString(margin));
        public HintWriter EmitLeftMargin(double margin) => EmitTag(HintTag.MarginLeft, HintUtils.UnitsToString(margin));
        public HintWriter EmitRightMargin(double margin) => EmitTag(HintTag.MarginRight, HintUtils.UnitsToString(margin));
        public HintWriter EmitMonoSpace(double width) => EmitTag(HintTag.MSpace, HintUtils.UnitsToString(width));
        public HintWriter EmitSpace(double width) => EmitTag(HintTag.Space, HintUtils.UnitsToString(width));
        public HintWriter EmitVerticalOffset(double margin) => EmitTag(HintTag.VOffset, HintUtils.UnitsToString(margin));

        public HintWriter EmitRotate(float rotation) => EmitTag(HintTag.Rotate, rotation);

        public HintWriter EmitSubscript() => EmitTag(HintTag.Sub);
        public HintWriter EmitSuperscript() => EmitTag(HintTag.Sup);
        public HintWriter EmitPage() => EmitTag(HintTag.Page);
        public HintWriter EmitNoParse() => EmitTag(HintTag.NoParse);
        public HintWriter EmitNoBreak() => EmitTag(HintTag.NoBr);
        public HintWriter EmitItalics() => EmitTag(HintTag.Italics);
        public HintWriter EmitBold() => EmitTag(HintTag.Bold);
        public HintWriter EmitStrikethrough() => EmitTag(HintTag.Strikethrough);
        public HintWriter EmitUnderlined() => EmitTag(HintTag.Underlined);
        public HintWriter EmitLowercase() => EmitTag(HintTag.Lowercase);
        public HintWriter EmitUppercase() => EmitTag(HintTag.UpperCase);
        public HintWriter EmitAllCaps() => EmitTag(HintTag.AllCaps);
        public HintWriter EmitSmallCaps() => EmitTag(HintTag.SmallCaps);
        public HintWriter EmitTagEnd(HintTag tag) => Emit($"</{HintUtils.ConvertTag(tag)}>");

        public HintWriter EmitTag(HintTag tag, object tagValue = null, string attribute = null)
        {
            var tagStr = HintUtils.ConvertTag(tag);

            if (tagValue is null)
            {
                m_String += $"<{tagStr}>";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(attribute))
                {
                    m_String += $"<{tagStr}={tagValue}>";
                }
                else
                {
                    m_String += $"<{tagStr} {attribute}={tagValue}>";
                }
            }

            return this;
        }

        public HintWriter Emit(string str)
        {
            m_String += str;
            return this;
        }

        public HintWriter EmitNewLine(string str = null)
        {
            if (str != null)
                m_String += $"\n{str}";
            else
                m_String += "\n";

            return this;
        }

        public HintWriter EmitReplace(string value, string replacement)
        {
            m_String = m_String.Replace(value, replacement);
            return this;
        }

        public HintWriter AccessString(Action<string> accessor)
        {
            accessor?.Invoke(m_String);
            return this;
        }

        public HintWriter Clear()
        {
            m_String = "";
            return this;
        }

        public override string ToString()
        {
            return m_String;
        }
    }
}