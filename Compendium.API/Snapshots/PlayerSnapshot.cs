namespace Compendium.Snapshots
{
    public struct PlayerSnapshot
    {
        public ReferenceHub Player;

        public RoleSnapshot Role;

        public int Id;

        public string UserId;
        public string Nickname;
        public string DisplayName;

        public PlayerSnapshot(ReferenceHub hub)
        {
            Player = hub;
            Role = SnapshotHelper.SaveRole(hub);
            Id = hub.PlayerId;
            UserId = hub.UserId();
            Nickname = hub.Nick();
            DisplayName = hub.nicknameSync.DisplayName;
        }
    }
}