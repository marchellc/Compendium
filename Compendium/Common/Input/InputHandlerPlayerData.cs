using UnityEngine;

namespace Compendium.Common.Input
{
    public class InputHandlerPlayerData
    {
        public string TargetId { get; }
        public string Name { get; }

        public KeyCode Key { get; set; }

        public InputHandlerPlayerData(string targetId, string name, KeyCode newKey)
        {
            TargetId = targetId;
            Name = name;
            Key = newKey;
        }
    }
}