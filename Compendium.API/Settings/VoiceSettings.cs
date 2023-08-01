using PlayerRoles;

using System.ComponentModel;

namespace Compendium.Settings
{
    public class VoiceSettings
    {
        [Description("Maximum number of packets sent per 100 milliseconds.")]
        public int CustomRateLimit { get; set; } = 128;

        [Description("All SCP roles allowed to use the proximity chat.")]
        public RoleTypeId[] AllowedScpChat { get; set; } = new RoleTypeId[]
        {
            RoleTypeId.Scp049
        };

        [Description("Maximum distance for SCP proximity chat.")]
        public float ScpChatDistance { get; set; } = 20f;
    }
}