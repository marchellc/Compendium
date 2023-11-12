using BetterCommands;

using Compendium.Extensions;
using Compendium.Input;
using Compendium.Constants;
using Compendium.IO.Saving;

using helpers.Attributes;
using helpers.Extensions;
using helpers.Pooling.Pools;
using helpers;

using PlayerRoles;
using PlayerRoles.Spectating;

using System;
using System.Linq;

using VoiceChat;

namespace Compendium.Voice.Profiles.Scp
{
    public class ScpVoiceProfile : BaseProfile
    {
        private static SaveFile<CollectionSaveData<string>> _mutes;

        public ScpVoiceProfile(ReferenceHub owner) : base(owner) { }

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
        {
            Flag = NextFlag;
            Owner.Broadcast(Colors.LightGreen($"Voice přepnut na {TypeAndColor()} chat</b>"), 3, true);
        }

        public override void Disable()
        {
            base.Disable();
        }

        public override void Process(VoicePacket packet)
        {
            if (packet.Speaker.netId != Owner.netId)
                return;

            if (Owner.IsSCP())
            {
                if (packet.SenderChannel != VoiceChatChannel.Mimicry)
                    packet.SenderChannel = VoiceChatChannel.ScpChat;

                var destinations = DictionaryPool<ReferenceHub, VoiceChatChannel>.Pool.Get(packet.Destinations);

                foreach (var p in packet.Destinations)
                {
                    if (p.Key.netId == packet.Speaker.netId)
                        continue;

                    if (!destinations.ContainsKey(p.Key))
                        continue;

                    if (destinations[p.Key] is VoiceChatChannel.Mimicry)
                        continue;

                    if (p.Key.RoleId() is RoleTypeId.Overwatch
                        && Owner.IsSpectatedBy(p.Key)
                        && !_mutes.Data.Contains(p.Key.UserId()))
                    {
                        destinations[p.Key] = VoiceChatChannel.RoundSummary;
                        continue;
                    }

                    if (Flag is ScpVoiceFlag.ScpChatOnly)
                    {
                        if (!p.Key.IsSCP())
                        {
                            destinations[p.Key] = VoiceChatChannel.None;
                            continue;
                        }

                        destinations[p.Key] = VoiceChatChannel.ScpChat;
                        continue;
                    }
                    else if (Flag is ScpVoiceFlag.ProximityAndScpChat)
                    {
                        if (p.Key.IsSCP())
                        {
                            destinations[p.Key] = VoiceChatChannel.ScpChat;
                            continue;
                        }
                        else
                        {
                            if (!_mutes.Data.Contains(p.Key.UserId()))
                            {
                                if (Plugin.Config.VoiceSettings.AllowedScpChat.Contains(Owner.RoleId()))
                                {
                                    if (p.Key.Position().IsWithinDistance(Owner.Position(), 25f))
                                    {
                                        destinations[p.Key] = VoiceChatChannel.RoundSummary;
                                        continue;
                                    }
                                    else
                                    {
                                        destinations[p.Key] = VoiceChatChannel.None;
                                        continue;
                                    }
                                }
                                else
                                {
                                    destinations[p.Key] = VoiceChatChannel.None;
                                    continue;
                                }
                            }
                        }
                    }
                    else if (Flag is ScpVoiceFlag.ProximityChatOnly)
                    {
                        if (!_mutes.Data.Contains(p.Key.UserId()))
                        {
                            if (Plugin.Config.VoiceSettings.AllowedScpChat.Contains(Owner.RoleId()))
                            {
                                if (p.Key.Position().IsWithinDistance(Owner.Position(), 25f))
                                {
                                    destinations[p.Key] = VoiceChatChannel.RoundSummary;
                                    continue;
                                }
                                else
                                {
                                    destinations[p.Key] = VoiceChatChannel.None;
                                    continue;
                                }
                            }
                            else
                            {
                                destinations[p.Key] = VoiceChatChannel.None;
                                continue;
                            }
                        }
                    }
                }

                packet.Destinations.Clear();
                packet.Destinations.AddRange(destinations);

                DictionaryPool<ReferenceHub, VoiceChatChannel>.Pool.Push(destinations);
            }
        }

        public string TypeAndColor()
        {
            switch (Flag)
            {
                case ScpVoiceFlag.ScpChatOnly:
                    return Colors.Red("SCP");

                case ScpVoiceFlag.ProximityChatOnly:
                    return Colors.Green("Proximity");

                case ScpVoiceFlag.ProximityAndScpChat:
                    return $"{Colors.Red("SCP")} a {Colors.Green("Proximity")}";

                default:
                    return "";
            }
        }

        [Command("muteproximity", CommandType.PlayerConsole)]
        [CommandAliases("mprox", "mutep")]
        [Description("Mutes SCP proximity chat.")]
        private static string MuteProximityCommand(ReferenceHub sender)
        {
            _mutes ??= new SaveFile<CollectionSaveData<string>>(Directories.GetDataPath("SavedProximityMutes", "proxMutes"));

            if (_mutes.Data.Contains(sender.UserId()))
            {
                _mutes.Data.Remove(sender.UserId());
                _mutes.Save();

                return "SCP proximity chat unmuted.";
            }
            else
            {
                _mutes.Data.Add(sender.UserId());
                _mutes.Save();

                return "SCP proximity chat muted.";
            }
        }

        [Load]
        private static void Load()
        {
            if (!InputManager.TryGetHandler<ScpVoiceKeybind>(out _))
                InputManager.Register<ScpVoiceKeybind>();

            _mutes ??= new SaveFile<CollectionSaveData<string>>(Directories.GetDataPath("SavedProximityMutes", "proxMutes"));
        }
    }
}