namespace Compendium.Snapshots
{
    public interface ISnapshotCapture
    {
        SnapshotDataType Type { get; }

        ISnapshotData Capture(ReferenceHub hub);
    }
}