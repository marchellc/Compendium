namespace Compendium.Logging
{
    public struct LogParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }

        public LogParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}