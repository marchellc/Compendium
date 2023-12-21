using helpers.Attributes;
using helpers.Configuration;

using System.Collections.Generic;
using UnityEngine;

namespace Compendium.Positions
{
    public static class PositionHelper
    {
        public static ConfigHandler Config;

        [Config(Name = "Positions", Description = "A list of positions.")]
        public static List<Position> Positions = new List<Position>()
        {
            new Position(),
            new Position()
        };

        [Load]
        public static void Load()
        {
            Config = new ConfigHandler(Directories.GetDataPath("positions.ini", "positions"));
            Config.BindAll(typeof(PositionHelper));
            Config.Load();
        }
    }
}
