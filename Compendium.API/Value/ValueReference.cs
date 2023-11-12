using System;

namespace Compendium.Value
{
    public class ValueReference
    {
        public object Value { get; set; }

        public Type Type { get; }

        public ValueReference(object value, Type type)
        {
            Value = value;
            Type = type;
        }
    }
}