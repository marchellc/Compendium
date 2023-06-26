using UnityEngine;

namespace Compendium.Npc.Targeting
{
    public class PositionTarget : ITarget
    {
        public Vector3 Position { get; }
        public bool IsValid { get; } = true;

        public PositionTarget(Vector3 position) => Position = position;
    }
}