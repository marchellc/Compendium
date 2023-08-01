using UnityEngine;

namespace Compendium.Input
{
    public interface IInputHandler
    {
        KeyCode Key { get; }

        bool IsChangeable { get; }

        string Id { get; }

        void OnPressed(ReferenceHub player);
    }
}