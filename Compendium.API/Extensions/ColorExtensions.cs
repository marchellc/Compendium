using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Compendium.Extensions
{
    public static class ColorHelper
    {
        public static Color ParseColor(string html) => ColorUtility.TryParseHtmlString(html, out var color) ? color : Color.black;

        public static IReadOnlyDictionary<string, string> NorthwoodApprovedColorCodes { get; } = new Dictionary<string, string>
        {
            {"pink", "#FF96DE"},
            {"red", "#C50000"},
            {"white", "#FFFFFF"},
            {"brown", "#944710"},
            {"silver", "#A0A0A0"},
            {"light_green", "#32CD32"},
            {"crimson", "#DC143C"},
            {"cyan", "#00B7EB"},
            {"aqua", "#00FFFF"},
            {"deep_pink", "#FF1493"},
            {"tomato", "#FF6448"},
            {"yellow", "#FAFF86"},
            {"magenta", "#FF0090"},
            {"blue_green", "#4DFFB8"},
            {"orange", "#FF9966"},
            {"lime", "#BFFF00"},
            {"green", "#228B22"},
            {"emerald", "#50C878"},
            {"carmine", "#960018"},
            {"nickel", "#727472"},
            {"mint", "#98FB98"},
            {"army_green", "#4B5320"},
            {"pumpkin", "#EE7600"},
            {"black", "#000000"}
        };

        public static Color GetNorthwoodApprovedColor(string colorName) =>
            NorthwoodApprovedColorCodes.TryGetValue(colorName, out var colorCode) ? ParseColor(colorCode) : Color.black;

        public static string GetClosestNorthwoodColor(string color) => GetClosestNorthwoodColor(ParseColor(color));
        public static string GetClosestNorthwoodColor(this Color color) =>
            NorthwoodApprovedColorCodes.TryGetValue(GetClosestNorthwoodColorName(color), out var c) ? c : "#FFFFFF";

        public static string GetClosestNorthwoodColorName(string color) => GetClosestNorthwoodColorName(ParseColor(color));
        public static string GetClosestNorthwoodColorName(this Color color)
        {
            var hsv = color.Hsv();
            var value = hsv[0] * 36000 + hsv[1] * 100 + hsv[2];
            var closest = NorthwoodApprovedColorCodes
            .ToDictionary(k => k.Key, v => ParseColor(v.Value).Hsv())
            .OrderBy(e =>
            {
                var colorHsv = e.Value;
                return Mathf.Abs(colorHsv[0] * 36000 + colorHsv[1] * 100 + colorHsv[2] - value);
            }).FirstOrDefault();
            return closest.Key;
        }

        public static float[] Hsv(this Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);

            return new[] { h, s, v };
        }

        public static string ToHex(this Color color, bool includeHash = true, bool includeAlpha = true) => $"{(includeHash ? "#" : "")}{(includeAlpha ? ColorUtility.ToHtmlStringRGBA(color) : ColorUtility.ToHtmlStringRGB(color))}";
        public static string ToHex(this Color32 color, bool includeHash = true, bool includeAlpha = true) => ToHex((Color)color, includeHash, includeAlpha);

    }
}
