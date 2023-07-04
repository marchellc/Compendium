using helpers.Configuration.Ini;

using PlayerRoles;

using System.Collections.Generic;

namespace Compendium.Voice
{
    public static class VoiceConfigs
    {
        [IniConfig("Bypass Rate Limit", null, "Whether or not to bypass the base game's voice rate limit check.")]
        public static bool BypassRateLimit { get; set; }

        [IniConfig("Allow Overwatch Scp Chat", null, "Whether or not to allow players in Overwatch to hear the SCP they are currently spectating.")]
        public static bool AllowOverwatchScpChat { get; set; } = true;

        [IniConfig("Scp Proximity Distance", null, "The maximum distance for SCP's proximity voice chat to be heard.")]
        public static float ScpProximityDistance { get; set; } = 20f;

        [IniConfig("Scp Proximity List", null, "A list of SCPs that can use the proximity voice.")]
        public static List<RoleTypeId> ProximityScps { get; set; } = new List<RoleTypeId>()
        {
            RoleTypeId.Scp173,
            RoleTypeId.Scp049,
            RoleTypeId.Scp096,
            RoleTypeId.Scp939,
            RoleTypeId.Scp0492,
            RoleTypeId.Scp106
        };
    }
}