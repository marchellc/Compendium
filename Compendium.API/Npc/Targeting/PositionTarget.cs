using UnityEngine;

namespace Compendium.Npc.Targeting
{
    public class PositionTarget : NpcTarget
    {
        public override Vector3 Position { get; }
        public override bool IsValid { get; } = true;

        public PositionTarget(Vector3 position) => Position = position;
    }
}