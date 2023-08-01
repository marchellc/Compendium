using UnityEngine;

namespace Compendium.Input
{
    public class InputBinding
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        
        public KeyCode Key { get; set; }
    }
}