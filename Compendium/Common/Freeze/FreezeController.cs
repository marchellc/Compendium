using BetterCommands;
using BetterCommands.Management;
using BetterCommands.Permissions;

using Compendium.State;
using Compendium.State.Base;

using PlayerRoles.FirstPersonControl;

using UnityEngine;

namespace Compendium.Common.Freeze
{
    public class FreezeController : StateBase
    {
        private Vector3? m_ForcedPos;
        private readonly Vector3 m_ForcedRot = Vector3.zero;

        public override void OnActiveUpdated()
        {
            if (!IsActive)
                m_ForcedPos = null;
            else
                m_ForcedPos = Player.transform.position;
        }

        public override void OnUpdate()
        {
            if (!m_ForcedPos.HasValue)
                return;

            Player.TryOverridePosition(m_ForcedPos.Value, m_ForcedRot);
        }

        [Command("freeze", CommandType.RemoteAdmin, CommandType.PlayerConsole)]
        [Permission(PermissionLevel.Low)]
        public static string FreezeCommand(ReferenceHub sender, ReferenceHub target)
        {
            if (target.TryGetState<FreezeController>(out var freeze))
            {
                if (freeze.IsActive)
                {
                    freeze.SetActive(false);
                    return $"Unfroze {target.LoggedNameFromRefHub()}";
                }
                else
                {
                    freeze.SetActive(true);
                    return $"Froze {target.LoggedNameFromRefHub()}";
                }
            }
            else
            {
                freeze.SetActive(true);
                return $"Froze {target.LoggedNameFromRefHub()}";
            }
        }
    }
}