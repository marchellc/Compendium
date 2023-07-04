namespace Compendium.Logging
{
    public struct DebugParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public DebugParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}