using System;

namespace Compendium.Snapshots
{
    public interface ISnapshotData
    {
        DateTime Time { get; }

        SnapshotDataType Type { get; }

        void Apply(ReferenceHub target);
    }
}