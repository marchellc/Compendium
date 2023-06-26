using System.Collections.Generic;
using System.ComponentModel;

namespace Compendium.Settings
{
    public class FeatureSettings
    {
        [Description("A list of disabled features.")]
        public List<string> Disabled { get; set; } = new List<string>();
    }
}