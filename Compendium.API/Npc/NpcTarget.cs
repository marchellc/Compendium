using UnityEngine;

namespace Compendium.Npc
{
    public class NpcTarget
    {
        public virtual Vector3 Position { get; }
        public virtual bool IsValid { get; }
    }
}