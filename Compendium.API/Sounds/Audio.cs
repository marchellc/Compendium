using BetterCommands;
using Compendium.Npc;
using Compendium.Round;

using helpers.Attributes;
using helpers.Extensions;
using helpers.IO.Storage;
using helpers.Pooling;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using UnityEngine;

using Utils.NonAllocLINQ;

using VoiceChat;

using Xabe.FFmpeg;

namespace Compendium.Sounds
{
    public static class Audio
    {
        internal static readonly HashSet<AudioPlayer> _activePlayers = new HashSet<AudioPlayer>();
        internal static readonly Dictionary<ReferenceHub, AudioPlayer> _ownedPlayers = new Dictionary<ReferenceHub, AudioPlayer>();

        internal static SingleFileStorage<Dictionary<string, HashSet<string>>> _mutes;

        public static Dictionary<string, HashSet<string>> Mutes
        {
            get
            {
                if (!_mutes.Data.Any())
                {
                    _mutes.Append(new Dictionary<string, HashSet<string>>());
                    _mutes.Save();

                    return _mutes.Data.First();
                }
                else
                    return _mutes.Data.First();
            }
        }

        [Load]
        [Reload]
        private static void Load()
        {
            if (_mutes != null)
            {
                _mutes.Reload();
                return;
            }

            _mutes = new SingleFileStorage<Dictionary<string, HashSet<string>>>($"{Directories.ThisData}/SavedAudioMutes");
            _mutes.Load();

            if (!_mutes.Data.Any())
                _mutes.Add(new Dictionary<string, HashSet<string>>());

            FFmpeg.SetExecutablesPath(AudioStore.DirectoryPath);
        }

        [Unload]
        private static void Unload()
        {
            if (_mutes != null)
                _mutes.Save();
        }

        [RoundStateChanged(RoundState.Ending)]
        private static void OnRoundEnd()
        {
            _ownedPlayers.ForEachValue(pl => PoolablePool.Push(pl));
            _ownedPlayers.Clear();
            _activePlayers.Clear();
        }

        public static AudioPlayer Play(string id, Vector3 position, string name = "default")
        {
            var pooledPlayer = PoolablePool.Get<AudioPlayer>();

            Action onFinished = () =>
            {
                if (pooledPlayer._speaker != null && pooledPlayer._speaker.TryGetNpc(out var npc) && npc is NpcBase npcBase)
                    PoolablePool.Push(npcBase);

                PoolablePool.Push(pooledPlayer);
            };

            pooledPlayer._speaker = PoolablePool.Get<NpcBase>().Hub;
            pooledPlayer.Name = name;
            pooledPlayer.Channel = VoiceChatChannel.Proximity;
            pooledPlayer.ChannelMode = VoiceChatChannel.None;
            pooledPlayer.Position.Value = position;
            pooledPlayer.OnFinishedTrack.Register(onFinished);
            pooledPlayer.Queue(id, null);

            return pooledPlayer;
        }

        [Command("instantplay", CommandType.RemoteAdmin)]
        [CommandAliases("iplay")]
        [Description("Uses a pooled player to instantly play your request.")]
        private static string InstantPlay(ReferenceHub sender, string query)
        {
            Play(query, sender.Position());
            return "Done.";
        }

        [Command("instanttargetplay", CommandType.RemoteAdmin)]
        [CommandAliases("itplay")]
        [Description("Uses a pooled player to instantly play your request at a targeted player.")]
        private static string InstantTargetPlay(ReferenceHub sender, ReferenceHub target, string query)
        {
            Play(query, target.Position());
            return "Done.";
        }

        [Command("play", CommandType.RemoteAdmin)]
        [Description("Starts playing a song!")]
        private static string PlayCommand(ReferenceHub sender, string query)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            player.Queue(query, str => sender?.Message(str, true));
            return "Request queued.";
        }

        [Command("volume", CommandType.RemoteAdmin)]
        [Description("Allows you to manage volume of your audio player.")]
        private static string VolumeCommand(ReferenceHub sender, float volume)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            player.Volume = volume;
            return $"Volume set to {volume}";
        }

        [Command("pause", CommandType.RemoteAdmin)]
        [Description("Pauses playback of your audio player.")]
        private static string PauseCommand(ReferenceHub sender)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
                return "You don't have any active audio players.";

            if (player.IsPaused)
                return "Your audio player is already paused!";

            player.IsPaused = true;
            return "Paused your audio player.";
        }

        [Command("resume", CommandType.RemoteAdmin)]
        [Description("Resumes playback of your audio player.")]
        private static string ResumeCommand(ReferenceHub sender)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
                return "You don't have any active audio players.";

            if (!player.IsPaused)
                return "Your audio player is not paused!";

            player.IsPaused = false;
            return "Resumed your audio player.";
        }

        [Command("discard", CommandType.RemoteAdmin)]
        [Description("Discards your audio player.")]
        private static string DiscardCommand(ReferenceHub sender)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
                return "You don't have any active audio players.";

            _ownedPlayers.Remove(sender);
            PoolablePool.Push(player);

            return "Discarded your audio player.";
        }

        [Command("stop", CommandType.RemoteAdmin)]
        [Description("Stops your audio player.")]
        private static string StopCommand(ReferenceHub sender)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
                return "You don't have any active audio players.";

            player.Stop();
            return "Stopped your audio player.";
        }

        [Command("channel", CommandType.RemoteAdmin)]
        [Description("Changes the playback channel of your audio player.")]
        private static string ChannelCommand(ReferenceHub sender, VoiceChatChannel channel)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            player.Channel = channel;
            return $"Set channel of your audio player to {player.Channel}";
        }

        [Command("channelmode", CommandType.RemoteAdmin)]
        [Description("Changes the playback channel's mode of your audio player.")]
        private static string ChannelModeCommand(ReferenceHub sender, VoiceChatChannel channel)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            player.ChannelMode = channel;
            return $"Set channel mode of your audio player to {player.Channel}";
        }

        [Command("loop", CommandType.RemoteAdmin)]
        [Description("Changes the loop mode of your audio player.")]
        private static string LoopCommand(ReferenceHub sender)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            player.IsLooping = !player.IsLooping;
            return player.IsLooping ? "Looping enabled." : "Looping disabled.";
        }

        [Command("audiowhitelist", CommandType.RemoteAdmin)]
        [CommandAliases("audiow", "awh")]
        [Description("Adds/removes a player from your audio player's whitelist.")]
        private static string WhitelistCommand(ReferenceHub sender, ReferenceHub target)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            if (player.IsWhitelisted(target))
            {
                player.RemoveWhitelist(target);
                return $"Removed whitelist for {target.Nick()}";
            }
            else
            {
                player.AddWhitelist(target);
                return $"Added whitelist for {target.Nick()}";
            }
        }

        [Command("audioblacklist", CommandType.RemoteAdmin)]
        [CommandAliases("audiobl", "abl")]
        [Description("Adds/removes a player from your audio player's blacklist.")]
        private static string BlacklistCommand(ReferenceHub sender, ReferenceHub target)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            if (player.IsBlacklisted(target))
            {
                player.RemoveBlacklist(target);
                return $"Removed blacklist for {target.Nick()}";
            }
            else
            {
                player.AddBlacklist(target);
                return $"Added blacklist for {target.Nick()}";
            }
        }

        [Command("audiosource", CommandType.RemoteAdmin)]
        [CommandAliases("asource")]
        [Description("Sets the audio source of your audio player.")]
        private static string SourceCommand(ReferenceHub sender, ReferenceHub target)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            player._speaker = target;
            return $"Set audio source to {target.Nick()}";
        }

        [Command("audiohostsource", CommandType.RemoteAdmin)]
        [CommandAliases("ahsource")]
        [Description("Sets the audio source of your audio player to the host player.")]
        private static string HostSourceCommand(ReferenceHub sender)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            player._speaker = ReferenceHub.HostHub;
            return $"Set audio source to the host player.";
        }

        [Command("audionpcsource", CommandType.RemoteAdmin)]
        [CommandAliases("anpcsource")]
        [Description("Sets the audio source of your audio player to an NPC.")]
        private static string NpcSourceCommand(ReferenceHub sender, string npcId)
        {
            if (!_ownedPlayers.TryGetValue(sender, out var player))
            {
                sender.Message("You don't have any active audio players .. hold on.", true);

                player = (_ownedPlayers[sender] = PoolablePool.Get<AudioPlayer>());
                player.Name = $"{sender.Nick()}'s audio player";
                player._speaker = sender;

                sender.Message($"Created a new audio player.", true);
            }

            if (!NpcManager.All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return $"Failed to find an NPC with ID '{npcId}'";

            player._speaker = npc.Hub;
            return $"Set audio source to the host player.";
        }

        [Command("download", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("adown", "audiod")]
        [Description("Downloads an audio file.")]
        private static string DownloadCommand(ReferenceHub sender, string query, string id)
        {
            if (query.StartsWith("http"))
            {
                if (query.Contains("yt") || query.Contains("youtu"))
                {
                    AudioUtils.Download(query, id, false);
                    return "Searching .. (YouTube - direct)";
                }
                else
                {
                    AudioUtils.Download(query, id, true);
                    return "Downloading .. (other)";
                }
            }
            else
            {
                AudioUtils.Download(query, id, false);
                return "Searching .. (YouTube - search)";
            }
        }

        [Command("audiomute", CommandType.PlayerConsole)]
        [CommandAliases("amute")]
        [Description("Mutes audio from the selected source.")]
        private static string AudioMuteCommand(ReferenceHub hub, string source)
        {
            if (Mutes.TryGetValue(hub.UserId(), out var list))
            {
                if (list.Contains(source))
                {
                    list.Remove(source);
                    _mutes.Save();
                    return $"Unmuted source '{source}'";
                }
                else
                {
                    list.Add(source);
                    _mutes.Save();
                    return $"Muted source '{source}'";
                }
            }
            else
            {
                Mutes[hub.UserId()] = new HashSet<string>() { source };
                _mutes.Save();
                return $"Muted source '{source}'";
            }
        }

        [Command("audiosources", CommandType.RemoteAdmin, CommandType.GameConsole, CommandType.PlayerConsole)]
        private static string ListAudioSourcesCommand(ReferenceHub sender)
        {
            var sb = new StringBuilder();

            if (!_activePlayers.Any())
                sb.AppendLine("There aren't any active sources.");
            else
                _activePlayers.For((i, source) => sb.AppendLine($"[{i + 1}] {source.Name} ({source.Status})"));

            return sb.ToString();
        }
    }
}