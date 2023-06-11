using CommandSystem;

using PluginAPI.Core;

using UnityEngine;

namespace Compendium.Npc.Targeting
{
    public class PlayerTarget : ITarget
    {
        private Player m_Target;

        public PlayerTarget(Player player)
        {
            m_Target = player;
        }

        public PlayerTarget(ICommandSender commandSender)
        {
            m_Target = Player.Get(commandSender);
        }

        public PlayerTarget(ReferenceHub hub)
        {
            m_Target = Player.Get(hub);
        }

        public Vector3 Position => m_Target.Position;
        public bool IsValid => m_Target != null && m_Target.IsAlive;
    }
}