using BetterCommands;
using BetterCommands.Management;
using BetterCommands.Permissions;

using Compendium.State;
using Compendium.State.Base;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;

using PlayerStatsSystem;

using UnityEngine;

namespace Compendium.Common.Rocket
{
    public class RocketController : StateBase
    {
        private Vector3 m_Start;
        private Vector3 m_StartRot;

        private float m_MaxHeight = 1800f;
        private float m_Add = 0.5f;

        private bool m_Activity;

        public override string Name => "Rocket";
        public override StateFlags Flags => StateFlags.RemoveOnRoleChange;

        public override void HandlePlayerDeath(DamageHandlerBase damageHandler)
        {
            if (!(damageHandler is DisruptorDamageHandler))
                return;

            SetActive(false);
        }

        public override void OnUpdate()
        {
            if (!Player.IsAlive())
                return;

            if (!m_Activity)
            {
                m_Start = Player.transform.position;
                m_StartRot = Player.transform.rotation.eulerAngles;
                m_Activity = true;
            }

            var newPos = Player.transform.position;
            newPos.y += m_Add;

            if (newPos.y >= m_MaxHeight)
            {
                Player.playerStats.KillPlayer(new DisruptorDamageHandler(new Footprinting.Footprint(Player), 9999f));
                SetActive(false);
                return;
            }

            Player.TryOverridePosition(newPos, m_StartRot);
        }

        [Command("rocket", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Permission(PermissionLevel.Low)]
        public static string RocketCommand(ReferenceHub sender, ReferenceHub target)
        {
            if (target.TryGetState<RocketController>(out var rocket))
            {
                rocket.SetActive(true);
                return $"Sent {target.nicknameSync.MyNick} into space!";
            }
            else
            {
                rocket = target.AddState<RocketController>();
                rocket.SetActive(true);
                return $"Sent {target.nicknameSync.MyNick} into space!";
            }
        }
    }
}