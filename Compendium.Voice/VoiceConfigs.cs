using helpers.Configuration.Ini;

using PlayerRoles;

using System.Collections.Generic;

using VoiceChat;

namespace Compendium.Voice
{
    public static class VoiceConfigs
    {
        [IniConfig(Name = "Bypass Rate Limit", Description = "Whether or not to bypass the base game's voice rate limit check.")]
        public static bool BypassRateLimit { get; set; }

        [IniConfig(Name = "Allow Overwatch Scp Chat", Description = "Whether or not to allow players in Overwatch to hear the SCP they are currently spectating.")]
        public static bool AllowOverwatchScpChat { get; set; } = true;

        [IniConfig(Name = "Update Hints", Description = "Whether or not to update hints that display information relevant to who's currently speaking, etc.")]
        public static bool UpdateHints { get; set; } = true;

        [IniConfig(Name = "Scp Proximity Distance", Description = "The maximum distance for SCP's proximity voice chat to be heard.")]
        public static float ScpProximityDistance { get; set; } = 20f;

        [IniConfig(Name = "Scp Proximity Channel", Description = "The channel to use for SCP proximity chat.")]
        public static VoiceChatChannel ProximityChannel { get; set; } = VoiceChatChannel.Proximity;

        [IniConfig(Name = "Scp Proximity List", Description = "A list of SCPs that can use the proximity voice.")]
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