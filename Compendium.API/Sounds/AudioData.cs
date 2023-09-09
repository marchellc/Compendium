namespace Compendium.Sounds
{
    public class AudioData
    {
        public string Source { get; }
        public string Id { get; }

        public byte[] Data { get; set; }

        public bool RequiresConversion { get; }

        public AudioData(string source, string id, byte[] data, bool convOverride = true)
        {
            Source = source;
            Id = id;
            Data = data;
            RequiresConversion = convOverride;
        }
    }
}