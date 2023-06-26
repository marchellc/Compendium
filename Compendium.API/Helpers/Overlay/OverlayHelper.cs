using helpers.Extensions;
using helpers.Pooling.Pools;

using PluginAPI.Core;

using System.Collections.Generic;
using System.Linq;

using Utils.NonAllocLINQ;

namespace Compendium.Helpers.Overlay
{
    public static class OverlayHelper
    {
        public static readonly OverlayPart MsgOverlayPart = new OverlayPart("$msg", -1f, false);

        public static void RebuildOverlay(ref string overlay, IEnumerable<OverlayPart> parts)
        {
            Log.Debug($"Overlay rebuilding started - {parts.Count()} parts", true, $"Overlay Helper");

            overlay = "";

            Log.Debug($"Overlay value reset", true, $"Overlay Helper");

            var partList = parts.ToList();

            var positions = DictionaryPool<OverlayPosition, List<string>>.Pool.Get();

            partList.Add(MsgOverlayPart);
            partList.ForEach(part =>
            {
                if (!positions.ContainsKey(part.Position))
                    positions.Add(part.Position, ListPool<string>.Pool.Get());

                Log.Debug($"Adding one part to {part.Position}", true, $"Overlay Helper");

                positions[part.Position].Add(part.Data());

                Log.Debug($"Added part: {positions[part.Position].Last()}", true, $"Overlay Helper");
            });

            Log.Debug($"Parts: {positions.Count}", true, $"Overlay Helper");

            foreach (var position in positions)
            {
                Log.Debug($"Translating position: {position.Key}", true, $"Overlay Helper");

                overlay += TranslatePositionStart(position.Key);

                Log.Debug($"Current: {overlay}", true, $"Overlay Helper");

                foreach (var ov in position.Value)
                {
                    Log.Debug($"Attaching overlay part: {ov}", true, $"Overlay Helper");
                    overlay += ov;
                }

                Log.Debug($"Current (2): {overlay}", true, $"Overlay Helper");

                overlay += TranslatePositionEnd(position.Key);

                Log.Debug($"Translated end: {overlay}", true, $"Overlay Helper");
            }

            Log.Debug($"Overlay rebuilding finished", true, $"Overlay Helper");

            positions.ForEachValue(val => ListPool<string>.Pool.Push(val));
            DictionaryPool<OverlayPosition, List<string>>.Pool.Push(positions);
        }

        private static string TranslatePositionStart(OverlayPosition overlayPosition)
        {
            switch (overlayPosition)
            {
                case OverlayPosition.Center: return "<align=center>";
                case OverlayPosition.CenterLeft: return "<align=left>";
                case OverlayPosition.CenterRight: return "<align=right>";

                case OverlayPosition.UpperCenter: return "<align=center><voffset=2em>";
                case OverlayPosition.UpperLeft: return "<align=left><voffset=-2em>";
                case OverlayPosition.UpperRight: return "<align=right><voffset=-2em>";

                case OverlayPosition.BottomCenter: return "<align=center><voffset=-2em>";
                case OverlayPosition.BottomLeft: return "<align=left><voffset=-2em>";
                case OverlayPosition.BottomRight:  return "<align=right><voffset=-2em>";          
            }

            return "";
        }

        private static string TranslatePositionEnd(OverlayPosition overlayPosition)
        {
            switch (overlayPosition)
            {
                case OverlayPosition.Center: return "</align>";
                case OverlayPosition.CenterLeft: return "</align>";
                case OverlayPosition.CenterRight: return "</align>";

                case OverlayPosition.UpperCenter: return "</voffset></align>";
                case OverlayPosition.UpperLeft: return "</voffset></align>";
                case OverlayPosition.UpperRight: return "</voffset></align>";

                case OverlayPosition.BottomCenter: return "</voffset></align>";
                case OverlayPosition.BottomLeft: return "</voffset></align>";
                case OverlayPosition.BottomRight: return "</voffset></align>";
            }

            return "";
        }
    }
}