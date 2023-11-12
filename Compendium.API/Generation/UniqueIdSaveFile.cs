using Compendium.IO.Saving;

using System.Collections.Generic;
using System.IO;

namespace Compendium.Generation
{
    public class UniqueIdSaveFile : SaveData
    {
        public List<string> IDs { get; } = new List<string>();

        public override bool IsBinary => false;

        public override void Read(StreamReader reader)
        {
            IDs.Clear();

            base.Read(reader);

            string line = null;

            while ((line = reader.ReadLine()) != null) 
                IDs.Add(line);
        }

        public override void Write(StreamWriter writer)
        {
            base.Write(writer);

            foreach (var id in IDs)
                writer.WriteLine(id);
        }
    }
}