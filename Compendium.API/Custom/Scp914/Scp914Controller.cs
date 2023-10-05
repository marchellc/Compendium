using helpers.Attributes;
using helpers.Configuration;

using System.Collections.Generic;

namespace Compendium.Custom.Scp914
{
    public static class Scp914Controller
    {
        private static ConfigHandler _scpConfig;

        [Config(Name = "Rough Recipes", Description = "A list of SCP-914's recipes for the Rough setting.")]
        public static Dictionary<ItemType, Dictionary<string, Dictionary<ItemType, int>>> RoughRecipes { get; } = Scp914Defaults.RoughDefaults;

        [Load]
        private static void Load()
        {
            if (_scpConfig != null)
            {
                _scpConfig.Load();
                return;
            }

            _scpConfig = new ConfigHandler(Directories.GetDataPath("Recipes.ini", "scp_recipes"));
            _scpConfig.BindAll(typeof(Scp914Controller));
            _scpConfig.Load();
        }
    }
}