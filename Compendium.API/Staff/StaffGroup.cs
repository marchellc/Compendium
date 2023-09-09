namespace Compendium.Staff
{
    public class StaffGroup
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Color { get; set; }
        
        public PlayerPermissions[] Permissions { get; set; }

        public bool IsCover { get; set; }
        public bool IsHidden { get; set; }
        public bool IsStaff { get; set; }
    }
}