using System;

using UnityEngine;

namespace Compendium.Input
{
    public class InputHandlerData
    {
        public string Name { get; }

        public KeyCode Key { get; }

        public Action<ReferenceHub> Listener { get; }

        public InputHandlerData(string name, KeyCode key, Action<ReferenceHub> listener)
        {
            Name = name;
            Key = key;
            Listener = listener;
        }
    }
}