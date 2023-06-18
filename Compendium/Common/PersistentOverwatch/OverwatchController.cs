using Compendium.Attributes;
using Compendium.Helpers.Events;
using Compendium.State.Base;

using PlayerRoles;

using PluginAPI.Enums;
using PluginAPI.Events;

using System;
using System.Collections.Generic;

namespace Compendium.Common.PersistentOverwatch
{
    public class OverwatchController : RequiredStateBase
    {
        private static readonly HashSet<string> m_Keep = new HashSet<string>();

        public override string Name => "Persistent Overwatch";

        [InitOnLoad]
        public static void Initialize()
        {
            ServerEventType.WaitingForPlayers.AddHandler<Action>(OnWaiting);
        }

        public override void HandlePlayerSpawn(RoleTypeId newRole)
        {
            if (newRole is RoleTypeId.Overwatch)
            {
                m_Keep.Add(Player.characterClassManager.UserId);
            }
            else
            {
                if (Player.GetRoleId() is RoleTypeId.Overwatch)
                {
                    m_Keep.Remove(Player.characterClassManager.UserId);
                }
            }
        }

        public static void OnWaiting()
        {
            foreach (var hub in ReferenceHub.AllHubs)
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    continue;

                if (m_Keep.Contains(hub.characterClassManager.UserId))
                    hub.roleManager.ServerSetRole(RoleTypeId.Overwatch, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
            }
        }
    }
}
