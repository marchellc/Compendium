using System;
using UnityEngine;

namespace Compendium.Common.Input
{
    public class InputHandlerData
    {
        public string Name { get; }

        public KeyCode DefaultKey { get; }

        public Action<ReferenceHub, KeyCode> OnReceived { get; }

        public InputHandlerData(string name, KeyCode key, Action<ReferenceHub, KeyCode> onReceived)
        {
            Name = name;
            DefaultKey = key;
            OnReceived = onReceived;
        }

        public void Receive(ReferenceHub sender, KeyCode key)
        {
            OnReceived?.Invoke(sender, key);
        }
    }
}