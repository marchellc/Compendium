using Compendium.Extensions;
using Compendium.Input;

using helpers.Extensions;

using PlayerRoles;

using VoiceChat;

namespace Compendium.Voice.Profiles.Scp
{
    public class ScpVoiceProfile : BaseProfile
    {
        public ScpVoiceProfile(ReferenceHub owner) : base(owner)
        {
            if (!InputManager.TryGetHandler<ScpVoiceKeybind>(out _))
                InputManager.Register<ScpVoiceKeybind>();
        }

        public ScpVoiceFlag Flag { get; set; } = ScpVoiceFlag.ScpChatOnly;

        public ScpVoiceFlag NextFlag
        {
            get
            {
                if (Flag is ScpVoiceFlag.ScpChatOnly)
                    return ScpVoiceFlag.ProximityAndScpChat;

                if (Flag is ScpVoiceFlag.ProximityAndScpChat)
                    return ScpVoiceFlag.ProximityChatOnly;

                return ScpVoiceFlag.ScpChatOnly;
            }
        }

        public void OnSwitchUsed()
            => Flag = NextFlag;

        public override void Process(VoicePacket packet)
        {
            if (packet.Speaker.netId != Owner.netId)
                return;

            if (Owner.IsSCP())
            {
                packet.SenderChannel = VoiceChatChannel.ScpChat;
                packet.Destinations.ForEach(p =>
                {
                    if (p.Key.netId == packet.Speaker.netId)
                        return;

                    if (Flag is ScpVoiceFlag.ScpChatOnly)
                    {
                        if (!p.Key.IsSCP())
                        {
                            packet.Destinations[p.Key] = VoiceChatChannel.None;
                            return;
                        }

                        packet.Destinations[p.Key] = VoiceChatChannel.ScpChat;
                    }
                    else if (Flag is ScpVoiceFlag.ProximityAndScpChat)
                    {
                        if (p.Key.IsSCP())
                            packet.Destinations[p.Key] = VoiceChatChannel.ScpChat;
                        else
                        {
                            if (Plugin.Config.VoiceSettings.AllowedScpChat.Contains(Owner.RoleId()))
                            {
                                if (p.Key.Position().IsWithinDistance(Owner.Position(), Plugin.Config.VoiceSettings.ScpChatDistance))
                                {
                                    packet.Destinations[p.Key] = VoiceChatChannel.RoundSummary;
                                }
                                else
                                {
                                    packet.Destinations[p.Key] = VoiceChatChannel.None;
                                }
                            }
                            else
                            {
                                packet.Destinations[p.Key] = VoiceChatChannel.None;
                            }
                        }
                    }
                    else
                    {
                        if (p.Key.IsSCP())
                            packet.Destinations[p.Key] = VoiceChatChannel.None;
                        else
                        {
                            if (Plugin.Config.VoiceSettings.AllowedScpChat.Contains(Owner.RoleId()))
                            {
                                if (p.Key.Position().IsWithinDistance(Owner.Position(), Plugin.Config.VoiceSettings.ScpChatDistance))
                                {
                                    packet.Destinations[p.Key] = VoiceChatChannel.RoundSummary;
                                }
                                else
                                {
                                    packet.Destinations[p.Key] = VoiceChatChannel.None;
                                }
                            }
                            else
                            {
                                packet.Destinations[p.Key] = VoiceChatChannel.None;
                            }
                        }
                    }
                });
            }
        }
    }
}