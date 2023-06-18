using PlayerRoles;

using System.Collections.Generic;
using System.ComponentModel;

namespace Compendium.Settings
{
    public class VoiceSettings
    {
        [Description("Proximity distances which can be configured for a specific team.")]
        public Dictionary<RoleTypeId, float> ProximityDistancing { get; set; } = new Dictionary<RoleTypeId, float>()
        {

        };

        [Description("A list of SCPs that are allowed to join the Proximity channel.")]
        public List<RoleTypeId> ScpsAllowedProximity { get; set; } = new List<RoleTypeId>()
        { 
            RoleTypeId.Scp049,
            RoleTypeId.Scp0492,
            RoleTypeId.Scp173,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp939
        };
    }
}