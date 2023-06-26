using UnityEngine;

namespace Compendium.Npc.Targeting
{
    public interface ITarget
    {
        Vector3 Position { get; }

        bool IsValid { get; }
    }
}