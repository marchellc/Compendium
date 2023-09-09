using helpers.Attributes;
using helpers.Configuration;

using System.Collections.Generic;
using System.IO;

namespace Compendium.Staff
{
    public static class StaffHandler
    {
        private static readonly Dictionary<string, string[]> _members = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, StaffGroup> _groups = new Dictionary<string, StaffGroup>();

        private static readonly Dictionary<ReferenceHub, StaffGroup> _active = new Dictionary<ReferenceHub, StaffGroup>();
        private static readonly Dictionary<StaffGroup, UserGroup> _groupMatrix = new Dictionary<StaffGroup, UserGroup>();

        private static ConfigHandler _config;

        public static string RolesFilePath => $"{Directories.ThisConfigs}/Staff Groups.txt";
        public static string MembersFilePath => $"{Directories.ThisConfigs}/Staff Members.txt";
        public static string ConfigFilePath => $"{Directories.ThisConfigs}/Staff Config.ini";

        [Load]
        [Reload]
        private static void Load()
        {
            if (_config != null)
                _config.Load();
            else
            {
                _config = new ConfigHandler(ConfigFilePath);
                _config.Load();
            }

            _members.Clear();
            _groups.Clear();
            _active.Clear();

            StaffReader.ReadMembers(_members);
            StaffReader.ReadGroups(_groups);

            ReassignGroups();
        }

        [Unload]
        private static void Unload()
        {
            StaffWriter.WriteGroups(_groups);
            StaffWriter.WriteMembers(_members);

            File.WriteAllLines(MembersFilePath, StaffWriter.MembersBuffer);
            File.WriteAllLines(RolesFilePath, StaffWriter.GroupsBuffer);

            StaffWriter.MembersBuffer = null;
            StaffWriter.GroupsBuffer = null;
        }

        public static void ReassignGroups()
        {

        }
    }
}