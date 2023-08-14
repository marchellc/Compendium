using Footprinting;

using helpers.Time;

using System;
using System.Collections.Generic;

namespace Compendium.Snapshots
{
    public class Snapshot
    {
        private Footprint _owner;
        private DateTime _time;

        private List<ISnapshotData> _data = new List<ISnapshotData>();

        public Footprint Owner => _owner;
        public DateTime Time => _time;

        public IReadOnlyCollection<ISnapshotData> Data => _data;

        public void Update(ReferenceHub player, SnapshotDataType[] includedData)
        {
            _owner = new Footprint(player);
            _time = TimeUtils.LocalTime;
            _data.Clear();

            if (includedData is null || includedData.IsEmpty())
                return;

            includedData.ForEach(type =>
            {
                if (SnapshotManager.TryGetCapture(type, out var capture))
                {
                    var snapshot = capture.Capture(player);

                    if (snapshot != null)
                        _data.Add(snapshot);
                }
            });
        }

        public void Apply(ReferenceHub player = null)
        {
            if (player is null)
                player = _owner.Hub;

            if (player is null)
                return;

            _data.ForEach(data => data.Apply(player));
        }

        public static Snapshot Capture(ReferenceHub hub, params SnapshotDataType[] includedData)
        {
            var ss = new Snapshot();
            ss.Update(hub, includedData);
            return ss;
        }
    }
}