namespace Compendium.Helpers.Hints
{
    public struct HintAlign
    {
        public readonly string Value;

        private HintAlign(string value) 
            => Value = value;

        public static readonly HintAlign Left = new HintAlign("left");
        public static readonly HintAlign Right = new HintAlign("right");
        public static readonly HintAlign Center = new HintAlign("center");
        public static readonly HintAlign Justify = new HintAlign("justified");
        public static readonly HintAlign Flush = new HintAlign("flush");
    }
}