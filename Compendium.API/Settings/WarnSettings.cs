using System.ComponentModel;

namespace Compendium.Settings
{
    public class WarnSettings
    {
        [Description("Whether or not to show a message to the target player.")]
        public bool Announce { get; set; } = true;
    }
}
