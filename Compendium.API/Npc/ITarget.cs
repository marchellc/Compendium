using UnityEngine;

namespace Compendium.Npc
{
    public interface ITarget
    {
        Vector3 Position { get; }

        bool IsValid { get; }
    }
}