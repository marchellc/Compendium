namespace Compendium.Health
{
    public struct CustomHealthData
    {
        public readonly bool KeepOnRole;
        public readonly float Value;

        public CustomHealthData(bool keepOnRole, float value)
        {
            KeepOnRole = keepOnRole;
            Value = value;
        }
    }
}