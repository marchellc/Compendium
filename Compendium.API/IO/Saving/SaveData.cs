using System.IO;

namespace Compendium.IO.Saving
{
    public class SaveData
    {
        public virtual bool IsBinary { get; }

        public virtual void Write(StreamWriter writer) { }
        public virtual void Write(BinaryWriter writer) { }

        public virtual void Read(BinaryReader reader) { }
        public virtual void Read(StreamReader reader) { }
    }
}