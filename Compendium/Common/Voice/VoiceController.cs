using Compendium.Attributes;
using Compendium.Common.Input;
using Compendium.Helpers.Staff;
using Compendium.State;
using Compendium.State.Base;

using PlayerRoles;
using PlayerRoles.Spectating;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using VoiceChat.Networking;

namespace Compendium.Common.Voice
{
    public class VoiceController : RequiredStateBase
    {
        private List<VoiceOverrides> m_Overrides = new List<VoiceOverrides>();
        private readonly VoiceData m_Data = new VoiceData();

        public override StateFlags Flags => StateFlags.DisableUpdate;
        public override string Name => "Voice Chat";

        public List<VoiceOverrides> Overrides => m_Overrides;

        public VoiceData Data => m_Data;

        [InitOnLoad]
        public static void Initialize()
        {
            InputHandler.TryAdd("voice_proximity_switch", KeyCode.AltGr, HandleProximity);
            InputHandler.TryAdd("voice_admin", KeyCode.RightAlt, HandleAdmin);
        }

        public override void HandlePlayerSpawn(RoleTypeId newRole)
        {
            if (m_Data.ShouldResetOnRole)
                m_Data.ResetAll();
        }

        public bool Receive(ReferenceHub speaker, ref VoiceMessage message, out bool shouldSend)
        {
            if (Player.netId == speaker.netId)
            {
                if (m_Overrides.Contains(VoiceOverrides.Playback))
                {
                    shouldSend = true;
                    return true;
                }
                else
                {
                    shouldSend = false;
                    return true;
                }
            }

            if (Player.GetRoleId() is RoleTypeId.Overwatch)
            {
                if (speaker.IsSpectatedBy(Player) || 
                    ReferenceHub.AllHubs.Any(hub => 
                                             hub.Mode is ClientInstanceMode.ReadyClient && 
                                             hub.IsSCP() && 
                                             hub.netId != speaker.netId &&
                                             hub.IsSpectatedBy(Player)))
                {
                    message.Channel = VoiceChat.VoiceChatChannel.ScpChat;
                    shouldSend = true;
                    return true;
                }
            }

            if (speaker.TryGetState<VoiceController>(out var vc))
            {
                if (vc.Data.IsActive)
                {
                    if (!vc.Data.IsAllowed(Player))
                    {
                        shouldSend = false;
                        return true;
                    }
                    else
                    {
                        if (vc.Data.IsOverrideActive)
                        {
                            message.Channel = vc.Data.Override;
                            shouldSend = true;
                            return true;
                        }
                    }
                }
            }

            shouldSend = false;
            return false;
        }

        private static void HandleProximity(ReferenceHub sender, KeyCode key)
        {
            if (!sender.IsSCP())
                return;

            if (sender.GetRoleId() is RoleTypeId.Scp079)
                return;

            if (sender.TryGetState<VoiceController>(out var vc))
            {
                vc.Data.Activate();
                vc.Data.ResetOnRoleChange();
                vc.Data.SetString("ScpChat");

                if (vc.Data.IsOverrideActive)
                {
                    if (vc.Data.Override is VoiceChat.VoiceChatChannel.Proximity)
                    {
                        vc.Data.SetOverride(VoiceChat.VoiceChatChannel.ScpChat);
                    }
                    else
                    {
                        vc.Data.SetOverride(VoiceChat.VoiceChatChannel.Proximity);
                    }
                }
                else
                {
                    vc.Data.SetOverride(VoiceChat.VoiceChatChannel.Proximity);
                }
            }
        }

        private static void HandleAdmin(ReferenceHub sender, KeyCode key)
        {
            if (!StaffHelper.IsConsideredStaff(sender))
                return;

            if (sender.TryGetState<VoiceController>(out var vc))
            {
                if (vc.IsActive)
                {
                    if (vc.Data.IsStringSet)
                    {
                        if (vc.Data.String is "AdminChat")
                        {
                            vc.Data.RemoveString();
                            vc.Data.RemoveOverride();
                            vc.Data.RemoveValidator();
                            vc.Data.Deactivate();
                        }
                    }
                }
                else
                {
                    vc.Data.Activate();
                    vc.Data.SetString("AdminChat");
                    vc.Data.SetOverride(VoiceChat.VoiceChatChannel.Spectator);
                    vc.Data.SetValidator(AdminChatValidator);
                }
            }
        }

        private static bool AdminChatValidator(ReferenceHub receiver) => StaffHelper.IsConsideredStaff(receiver);
    }
}