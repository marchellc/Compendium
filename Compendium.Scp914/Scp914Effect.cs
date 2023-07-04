namespace Compendium.Scp914
{
    public class Scp914Effect
    {
        public byte Intensity { get; set; } = 4;

        public float Duration { get; set; } = 5f;

        public bool AddDuration { get; set; }

        public string Effect { get; set; } = "default";
    }
}