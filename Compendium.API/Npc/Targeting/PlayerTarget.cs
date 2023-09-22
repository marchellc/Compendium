using CommandSystem;

using PluginAPI.Core;

using UnityEngine;

namespace Compendium.Npc.Targeting
{
    public class PlayerTarget : NpcTarget
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

        public override Vector3 Position => m_Target.Position;
        public override bool IsValid => m_Target != null && m_Target.ReferenceHub != null && !m_Target.IsServer && m_Target.IsAlive;
    }
}