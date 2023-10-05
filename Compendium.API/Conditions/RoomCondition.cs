using MapGeneration;

namespace Compendium.Conditions.Player
{
    public class RoomCondition : Condition
    {
        private RoomIdentifier _room;

        public RoomCondition(RoomIdentifier room)
            => _room = room;

        public override bool IsMatch(ReferenceHub hub)
        {
            var hubRoom = hub.Room();
            return _room is null || (hubRoom != null && hubRoom == _room);
        }
    }
}