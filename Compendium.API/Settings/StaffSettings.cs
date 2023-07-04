using System.Collections.Generic;
using System.ComponentModel;

namespace Compendium.Settings
{
    public class StaffSettings
    {
        [Description("A list of roles that are to be considered staff.")]
        public List<string> StaffRoles { get; set; } = new List<string>()
        {
            "owner",
            "admin",
            "moderator"
        };

        [Description("Whether or not to use a global members file.")]
        public bool UseGlobalFile { get; set; } = true;

        [Description("Whether or not to consider Northwood's global moderators as server staff.")]
        public bool GlobalModeratorsAreStaff { get; set; } = true;

        [Description("Whether or not to consider Northwood's staff members as server staff.")]
        public bool NorthwoodStaffAreServer { get; set; } = true;

        [Description("Whether or not to notify the server's staff (once they are verified).")]
        public bool NotifyStaff { get; set; } = true;

        [Description("Whether or not to automatically reload the staff file on change.")]
        public bool FileWatcher { get; set; } = true;
    }
}