namespace Compendium.Staff
{
    public class StaffKickPower
    {
        public byte Power { get; set; } = byte.MaxValue;
        public byte Required { get; set; } = byte.MaxValue;

        public bool CanKick(byte otherRequired)
            => Power >= otherRequired;
    }
}