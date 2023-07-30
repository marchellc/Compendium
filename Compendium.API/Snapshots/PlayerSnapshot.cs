using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compendium.Snapshots
{
    public struct PlayerSnapshot
    {
        public ReferenceHub Player;

        public int Id;

        public string UserId;
        public string Nickname;
        public string DisplayName;
    }
}
