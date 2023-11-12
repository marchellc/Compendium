using helpers.Network.Extensions.Data;

using System.IO;

namespace Compendium.IO.Saving
{
    public class SimpleSaveData<TValue> : SaveData
    {
        public TValue Value { get; set; }

        public override bool IsBinary => true;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            var obj = reader.ReadObject();

            if (obj is null)
                Value = default;
            else if (obj is not TValue tObj)
                throw new InvalidDataException($"Object type '{obj.GetType().FullName}' cannot be converted to {typeof(TValue).FullName}");
            else
                Value = tObj;
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.WriteObject(Value);
        }
    }
}