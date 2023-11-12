using System.Collections.Generic;

namespace Compendium.Spawning
{
    public class SpawnPartyInfo
    {
        public string InitiatedBy { get; }

        public List<string> Users { get; } 

        public SpawnPartyInfo(string initiatedBy)
        {
            InitiatedBy = initiatedBy;
            Users = new List<string>();
        }
    }
}