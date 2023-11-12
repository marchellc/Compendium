using helpers.Configuration;

namespace Compendium.Spawning
{
    public static class SpawnConfig
    {
        public static readonly ConfigHandler Config;

        static SpawnConfig()
        {
            Config = new ConfigHandler(Directories.GetDataPath("spawn.ini", "spawnConfig"));

            Config.BindAll(typeof(SpawnHandler));
            Config.BindAll(typeof(SpawnHistory));
            Config.BindAll(typeof(SpawnPartyHandler));
            Config.BindAll(typeof(SpawnPositionChooser));
            Config.BindAll(typeof(SpawnRoleChooser));
        }

        public static void Load()
            => Config.Load();
    }
}