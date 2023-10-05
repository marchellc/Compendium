using Compendium.Extensions;
using Compendium.PlayerData;
using Compendium.Custom.Stats.Health;
using Compendium.Scheduling;
using Compendium.Messages;
using Compendium;
using Compendium.Npc;
using Compendium.Enums;
using Compendium.Staff;
using Compendium.Comparison;
using Compendium.Snapshots;
using Compendium.Snapshots.Data;

using helpers;
using helpers.Extensions;
using helpers.Patching;
using helpers.Pooling.Pools;

using MapGeneration;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.Visibility;
using PlayerRoles.Voice;

using PlayerStatsSystem;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using InventorySystem.Items.Firearms;
using InventorySystem.Items;
using InventorySystem.Disarming;
using InventorySystem;

using RelativePositioning;

using Mirror;

using Respawning;

using CustomPlayerEffects;

using RoundRestarting;

using RemoteAdmin.Communication;
using RemoteAdmin;

using VoiceChat;
using Compendium.Attributes;

namespace Compendium
{
    public static class HubNetworkExtensions
    {
        public enum SoundId
        {
            Beep,
            GunShot,
            Lever,
        }

        private static readonly Dictionary<ReferenceHub, Vector3> _fakePositions = new Dictionary<ReferenceHub, Vector3>();
        private static readonly Dictionary<ReferenceHub, Dictionary<ReferenceHub, Vector3>> _fakePositionsMatrix = new Dictionary<ReferenceHub, Dictionary<ReferenceHub, Vector3>>();

        private static readonly Dictionary<ReferenceHub, Dictionary<Type, byte>> _fakeIntensity = new Dictionary<ReferenceHub, Dictionary<Type, byte>>();
        private static readonly Dictionary<ReferenceHub, RemoteAdminIconType> _raIcons = new Dictionary<ReferenceHub, RemoteAdminIconType>();

        private static readonly Dictionary<uint, List<uint>> _invisMatrix = new Dictionary<uint, List<uint>>();
        private static readonly HashSet<uint> _invisList = new HashSet<uint>();

        public static bool IsInvisibleTo(this ReferenceHub player, ReferenceHub target)
        {
            if (_invisList.Contains(player.netId))
                return true;

            return _invisMatrix.TryGetValue(player.netId, out var list) && list.Contains(target.netId);
        }

        public static bool IsInvisible(this ReferenceHub player)
            => _invisList.Contains(player.netId);

        public static void MakeInvisible(this ReferenceHub player)
            => _invisList.Add(player.netId);

        public static void MakeVisible(this ReferenceHub player)
            => _invisList.Remove(player.netId);

        public static void MakeInvisibleTo(this ReferenceHub player, ReferenceHub target)
        {
            if (_invisMatrix.TryGetValue(player.netId, out var list))
                list.Add(target.netId);
            else
                _invisMatrix[player.netId] = new List<uint>() { target.netId };
        }

        public static void MakeVisibleTo(this ReferenceHub player, ReferenceHub target)
        {
            if (_invisMatrix.TryGetValue(player.netId, out var list))
                list.Remove(target.netId);
        }

        public static bool TryGetFakePosition(this ReferenceHub hub, ReferenceHub target, out Vector3 position)
        {
            if (_fakePositions.TryGetValue(hub, out position))
                return true;

            if (target != null && _fakePositionsMatrix.TryGetValue(hub, out var matrix))
                return matrix.TryGetValue(target, out position);

            return false;
        }

        public static bool TryGetFakeIntensity(this ReferenceHub hub, Type type, out byte intensity)
        {
            if (_fakeIntensity.TryGetValue(hub, out var dict))
                return dict.TryGetValue(type, out intensity);

            intensity = 0;
            return false;
        }

        public static bool TryGetRaIcon(this ReferenceHub hub, out RemoteAdminIconType icon)
            => _raIcons.TryGetValue(hub, out icon);

        public static void FakeIntensity(this ReferenceHub hub, Type type, byte intensity)
        {
            if (!_fakeIntensity.ContainsKey(hub))
                _fakeIntensity[hub] = new Dictionary<Type, byte>();

            _fakeIntensity[hub][type] = intensity;
        }

        public static void FakePosition(this ReferenceHub hub, Vector3 position)
            => _fakePositions[hub] = position;

        public static void SetRaIcon(this ReferenceHub hub, RemoteAdminIconType icon)
            => _raIcons[hub] = icon;

        public static void FakePositionTo(this ReferenceHub hub, Vector3 position, params ReferenceHub[] targets)
        {
            if (!_fakePositionsMatrix.ContainsKey(hub))
                _fakePositionsMatrix[hub] = new Dictionary<ReferenceHub, Vector3>();

            targets.ForEach(target =>
            {
                _fakePositionsMatrix[hub][target] = position;
            });
        }

        public static void RemoveRaIcon(this ReferenceHub hub)
            => _raIcons.Remove(hub);

        public static void RemoveFakePosition(this ReferenceHub hub)
            => _fakePositions.Remove(hub);

        public static void RemoveAllFakePositions(this ReferenceHub hub)
        {
            _fakePositions.Remove(hub);
            _fakePositionsMatrix.Remove(hub);
        }

        public static void RemoveTargetFakePosition(this ReferenceHub hub, params ReferenceHub[] targets)
        {
            if (_fakePositionsMatrix.TryGetValue(hub, out var matrix))
            {
                targets.ForEach(target =>
                {
                    matrix.Remove(target);
                });
            }
        }

        public static void RemoveFakeIntensity(this ReferenceHub hub, Type type)
        {
            if (_fakeIntensity.ContainsKey(hub))
                return;

            _fakeIntensity[hub].Remove(type);
        }

        public static void PlaySound(this ReferenceHub hub, SoundId soundId, params object[] args)
        {
            switch (soundId)
            {
                case SoundId.Beep:
                    hub.SendFakeTargetRpc(null, typeof(AmbientSoundPlayer), nameof(AmbientSoundPlayer.RpcPlaySound), 7);
                    break;

                case SoundId.GunShot:
                    hub.connectionToClient.Send(new GunAudioMessage()
                    {
                        Weapon = (ItemType)args[0],
                        MaxDistance = (byte)args[1],
                        AudioClipId = (byte)args[2],
                        ShooterHub = hub,
                        ShooterPosition = new RelativePosition((Vector3)args[3])
                    });
                    break;

                case SoundId.Lever:
                    hub.SendFakeTargetRpc(hub.networkIdentity, typeof(PlayerInteract), nameof(PlayerInteract.RpcLeverSound));
                    break;
            }
        }

        public static void PlayBeepSound(this ReferenceHub hub)
            => hub.PlaySound(SoundId.Beep);

        public static void PlayGunSound(this ReferenceHub hub, ItemType weaponType, byte volume, byte id, Vector3 position)
            => hub.PlaySound(SoundId.GunShot, weaponType, volume, id, position);

        public static void PlayCassie(this ReferenceHub hub, string announcement, bool isHold = false, bool isNoisy = false, bool isSubtitles = false)
            => RespawnEffectsController.AllControllers.ForEach(ctrl =>
            {
                if (ctrl is null)
                    return;

                hub.SendFakeTargetRpc(ctrl.netIdentity, typeof(RespawnEffectsController), nameof(RespawnEffectsController.RpcCassieAnnouncement), announcement, isHold, isNoisy, isSubtitles);
            });

        public static void PlayCassie(this ReferenceHub hub, string words, string translation, bool isHold = false, bool isNoisy = true, bool isSubtitles = true)
        {
            var announcement = StringBuilderPool.Pool.Get();
            var cassies = words.Split('\n');
            var translations = translation.Split('\n');
            
            for (int i = 0; i < cassies.Length; i++)
                announcement.Append($"{translations[i]}<size=0> {cassies[i].Replace(' ', ' ')} </size><split>");

            var message = StringBuilderPool.Pool.PushReturn(announcement);

            RespawnEffectsController.AllControllers.ForEach(ctrl =>
            {
                if (ctrl is null)
                    return;

                hub.SendFakeTargetRpc(ctrl.netIdentity, typeof(RespawnEffectsController), nameof(RespawnEffectsController.RpcCassieAnnouncement), announcement, isHold, isNoisy, isSubtitles);
            });
        }

        public static void SetTargetInfo(this ReferenceHub hub, string info, params ReferenceHub[] targets)
            => targets.ForEach(target => hub.SendFakeSyncVar(target.networkIdentity, typeof(NicknameSync), nameof(NicknameSync.Network_customPlayerInfoString), info));

        public static void SetTargetRoomColor(this RoomLightController light, Color color, params ReferenceHub[] targets)
            => targets.ForEach(target =>
            {
                target.SendFakeSyncVar(light.netIdentity, typeof(RoomLightController), nameof(RoomLightController.NetworkOverrideColor), color);
                target.SendFakeSyncVar(light.netIdentity, typeof(RoomLightController), nameof(RoomLightController.NetworkLightsEnabled), true);
            });

        public static void SetTargetNickname(this ReferenceHub hub, string nick, params ReferenceHub[] targets)
            => targets.ForEach(target => target.SendFakeSyncVar(hub.networkIdentity, typeof(NicknameSync), nameof(NicknameSync.Network_displayName), nick));

        public static void SetTargetRole(this ReferenceHub hub, RoleTypeId role, byte unitId = 0, params ReferenceHub[] targets)
        {
            if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out var roleBase))
                return;

            bool isRisky = role.GetTeam() is Team.Dead || !hub.IsAlive();

            var writer = NetworkWriterPool.Get();

            writer.WriteUShort(38952);
            writer.WriteUInt(hub.netId);
            writer.WriteRoleType(role);

            if (roleBase is HumanRole humanRole && humanRole.UsesUnitNames)
            {
                if (!(hub.Role() is HumanRole))
                    isRisky = true;

                writer.WriteByte(unitId);
            }

            if (roleBase is FpcStandardRoleBase fpc)
            {
                if (!(hub.Role() is FpcStandardRoleBase playerFpc))
                    isRisky = true;
                else
                    fpc = playerFpc;

                fpc.FpcModule.MouseLook.GetSyncValues(0, out ushort value, out ushort _);

                writer.WriteRelativePosition(hub.RelativePosition());
                writer.WriteUShort(value);
            }

            if (roleBase is ZombieRole)
            {
                if (!(hub.Role() is ZombieRole))
                    isRisky = true;

                writer.WriteUShort((ushort)Mathf.Clamp(Mathf.CeilToInt(hub.MaxHealth()), ushort.MinValue, ushort.MaxValue));
            }

            var arraySegment = writer.ToArraySegment();

            targets.ForEach(target =>
            {
                if (target != hub || !isRisky)
                    target.connectionToClient.Send(arraySegment);
                else
                    Plugin.Warn($"Blocked a possible self-desync attempt of '{hub.Nick()}' with role '{role}'");
            });

            NetworkWriterPool.Return(writer);
            hub.Position(hub.Position() + (Vector3.up * 0.25f));
        }

        public static void SetTargetWarheadLevel(this ReferenceHub hub, bool isEnabled = true)
            => hub.SendFakeSyncVar(AlphaWarheadOutsitePanel.nukeside.netIdentity, typeof(AlphaWarheadNukesitePanel), nameof(AlphaWarheadNukesitePanel.Networkenabled), isEnabled);

        public static void SetTargetWarheadKeycard(this ReferenceHub hub, bool isEntered = true)
            => hub.SendFakeSyncVar(GameObject.Find("OutsitePanelScript").GetComponentInParent<AlphaWarheadOutsitePanel>().netIdentity, typeof(AlphaWarheadOutsitePanel), nameof(AlphaWarheadOutsitePanel.NetworkkeycardEntered), isEntered);

        public static void SetAspectRatio(this ReferenceHub hub, float ratio = 1f)
            => hub.aspectRatioSync.CmdSetAspectRatio(ratio);

        public static void SetTargetWindowStatus(this BreakableWindow window, BreakableWindow.BreakableWindowStatus status, params ReferenceHub[] targets)
            => targets.ForEach(target => target.SendFakeSyncVar(window.netIdentity, typeof(BreakableWindow), nameof(BreakableWindow.NetworksyncStatus), status));

        public static void SetTargetWarheadStatus(this ReferenceHub hub, AlphaWarheadSyncInfo status)
            => hub.SendFakeSyncVar(AlphaWarheadController.Singleton.netIdentity, typeof(AlphaWarheadController), nameof(AlphaWarheadController.NetworkInfo), status);

        public static void SetTargetServerName(this ReferenceHub hub, string name)
            => hub.SendFakeSyncVar(null, typeof(ServerConfigSynchronizer), nameof(ServerConfigSynchronizer.NetworkServerName), name);

        public static void SetTargetGlobalBadge(this ReferenceHub hub, string text, params ReferenceHub[] targets)
            => targets.ForEach(target => target.SendFakeSyncVar(hub.networkIdentity, typeof(ServerRoles), nameof(ServerRoles.NetworkGlobalBadge), text));

        public static void SetTargetRankColor(this ReferenceHub hub, string color, params ReferenceHub[] targets)
            => targets.ForEach(target => target.SendFakeSyncVar(hub.networkIdentity, typeof(ServerRoles), nameof(ServerRoles.Network_myColor), color));

        public static void SetTargetRankText(this ReferenceHub hub, string text, params ReferenceHub[] targets)
            => targets.ForEach(target => target.SendFakeSyncVar(hub.networkIdentity, typeof(ServerRoles), nameof(ServerRoles.Network_myText), text));

        public static void SetTargetRank(this ReferenceHub hub, string color, string text, params ReferenceHub[] targets)
        {
            hub.SetTargetRankColor(color, targets);
            hub.SetTargetRankText(text, targets);
        }

        public static void SetTargetMapSeed(this ReferenceHub hub, int seed)
            => hub.SendFakeSyncVar(SeedSynchronizer._singleton.netIdentity, typeof(SeedSynchronizer), nameof(SeedSynchronizer.Network_syncSeed), seed);

        public static void SetTargetMouseSpawn(this ReferenceHub hub, byte spawn)
            => hub.SendFakeSyncVar(UnityEngine.Object.FindObjectOfType<SqueakSpawner>().netIdentity, typeof(SqueakSpawner), nameof(SqueakSpawner.NetworksyncSpawn), spawn);

        public static void SetTargetChaosCount(this ReferenceHub hub, int count)
            => hub.SendFakeSyncVar(RoundSummary.singleton.netIdentity, typeof(RoundSummary), nameof(RoundSummary.Network_chaosTargetCount), count);

        public static void SetTargetIntercomText(this ReferenceHub hub, string text)
            => hub.SendFakeSyncVar(IntercomDisplay._singleton.netIdentity, typeof(IntercomDisplay), nameof(IntercomDisplay.Network_overrideText), text);

        public static void SetTargetIntercomState(this ReferenceHub hub, IntercomState state)
            => hub.SendFakeSyncVar(Intercom._singleton.netIdentity, typeof(Intercom), nameof(Intercom.Network_state), (byte)state);

        public static void SendWarheadShake(this ReferenceHub hub, bool achieve = true)
            => hub.SendFakeTargetRpc(AlphaWarheadController.Singleton.netIdentity, typeof(AlphaWarheadController), nameof(AlphaWarheadController.RpcShake), achieve);

        public static void SendHitmarker(this ReferenceHub hub, float size = 1f)
            => Hitmarker.SendHitmarker(hub, size);

        public static void SendDimScreen(this ReferenceHub hub)
            => hub.SendFakeTargetRpc(RoundSummary.singleton.netIdentity, typeof(RoundSummary), nameof(RoundSummary.RpcDimScreen));

        public static void SendShowRoundSummary(this ReferenceHub hub, 
            RoundSummary.SumInfo_ClassList startClassList, 
            RoundSummary.SumInfo_ClassList endClassList, 
            
            RoundSummary.LeadingTeam leadingTeam, 
            
            int escapedClassD, 
            int escapedScientists, 
            int scpKills, 
            int roundCd, 
            int durationSeconds)
            => hub.SendFakeTargetRpc(RoundSummary.singleton.netIdentity, typeof(RoundSummary), nameof(RoundSummary.RpcShowRoundSummary), 
                startClassList,
                endClassList,
                leadingTeam,
                escapedClassD,
                escapedScientists,
                scpKills,
                roundCd,
                durationSeconds);

        public static void SendCloseRemoteAdmin(this ReferenceHub hub)
            => hub.serverRoles.TargetCloseRemoteAdmin();

        public static void SendOpenRemoteAdmin(this ReferenceHub hub, bool isPassword = false)
            => hub.serverRoles.TargetOpenRemoteAdmin(isPassword);

        public static void SendHiddenRole(this ReferenceHub hub, string text)
            => hub.serverRoles.TargetSetHiddenRole(hub.connectionToClient, text);

        public static void SendTeslaTrigger(this TeslaGate gate, params ReferenceHub[] targets)
            => targets.ForEach(target => target.SendFakeTargetRpc(gate.netIdentity, typeof(TeslaGate), nameof(TeslaGate.RpcInstantBurst)));

        public static void SendRoundRestart(this ReferenceHub hub, bool shouldReconnect = true, bool extendedTime = false, bool isFast = false, float offset = 0f, ushort? redirect = null)
            => hub.connectionToClient.Send(new RoundRestartMessage(
                redirect.HasValue ? RoundRestartType.RedirectRestart : (isFast ? RoundRestartType.RedirectRestart : RoundRestartType.FullRestart),
                offset, redirect.HasValue ? redirect.Value : ushort.MinValue, shouldReconnect, extendedTime));

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnWaiting()
        {
            _fakePositions.Clear();
            _fakePositionsMatrix.Clear();
            _fakeIntensity.Clear();
            _raIcons.Clear();
            _invisList.Clear();
            _invisMatrix.Clear();
        }
    }

    public static class HubDataExtensions
    {
        public static string Nick(this ReferenceHub hub, string newNick = null)
        {
            if (string.IsNullOrWhiteSpace(newNick))
                return hub.nicknameSync.Network_myNickSync;

            hub.nicknameSync.SetNick(newNick);
            return newNick;
        }

        public static string DisplayNick(this ReferenceHub hub, string newDisplayNick = null)
        {
            if (string.IsNullOrWhiteSpace(newDisplayNick))
                return hub.nicknameSync._cleanDisplayName;

            hub.nicknameSync.Network_displayName = newDisplayNick;
            return newDisplayNick;
        }

        public static void ResetDisplayNick(this ReferenceHub hub)
            => hub.nicknameSync.Network_displayName = null;

        public static bool IsPlayer(this ReferenceHub hub)
            => hub.Mode is ClientInstanceMode.ReadyClient;

        public static bool IsServer(this ReferenceHub hub)
            => hub.Mode is ClientInstanceMode.DedicatedServer || hub.Mode is ClientInstanceMode.Host;

        public static bool IsVerified(this ReferenceHub hub)
            => hub.Mode != ClientInstanceMode.Unverified;

        public static bool IsNorthwoodStaff(this ReferenceHub hub)
            => hub.serverRoles.Staff || hub.serverRoles.RaEverywhere;

        public static bool IsNorthwoodModerator(this ReferenceHub hub)
            => hub.serverRoles.RaEverywhere;

        public static bool IsStaff(this ReferenceHub hub, bool countNwStaff = true)
        {
            if (hub.IsNorthwoodStaff())
                return countNwStaff;

            if (Plugin.Config.ApiSetttings.ConsiderRemoteAdminAccessAsStaff && hub.serverRoles.RemoteAdmin)
                return true;

            if (StaffHandler.Members.TryGetValue(hub.UserId(), out var groups))
                return groups.Any(g => StaffHandler.Groups.TryGetValue(g, out var group) && group.GroupFlags.Contains(StaffGroupFlags.IsStaff));

            return false;
        }

        public static string UserId(this ReferenceHub hub)
            => hub.characterClassManager.UserId;

        public static string UniqueId(this ReferenceHub hub)
            => PlayerDataRecorder.GetData(hub)?.Id ?? "";

        public static UserIdValue ParsedUserId(this ReferenceHub hub)
            => UserIdValue.TryParse(hub.UserId(), out var parsed) ? parsed : throw new Exception($"Failed to parse user ID of {hub.UserId()}");

        public static string UserId2(this ReferenceHub hub, string userId2 = null)
        {
            if (!string.IsNullOrWhiteSpace(userId2))
            {
                hub.characterClassManager.UserId2 = userId2;
                return userId2;
            }

            return hub.characterClassManager.UserId2;
        }

        public static string Ip(this ReferenceHub hub)
        {
            return hub.connectionToClient.address;
        }

        public static bool HasReservedSlot(this ReferenceHub hub, bool countBypass = false)
            => ReservedSlot.HasReservedSlot(hub.UserId(), out var bypass) || (countBypass && bypass);

        public static void AddReservedSlot(this ReferenceHub hub, bool isTemporary, bool addNick = false)
        {
            if (hub.HasReservedSlot())
                return;

            Plugin.Debug($"Added temporary reserved slot to {hub.GetLogName(true, false)}");

            ReservedSlot.Users.Add(hub.UserId());

            if (isTemporary)
                return;

            List<string> lines = new List<string>();

            if (addNick)
                lines.Add($"# Player: {hub.Nick()} ({hub.Ip()})");

            lines.Add($"{hub.UserId()}");

            File.AppendAllLines(PluginAPI.Core.ReservedSlots.FilePath, lines);

            ReservedSlot.Reload();

            Plugin.Debug($"Added permanent reserved slot to {hub.GetLogName(true, false)}");
        }

        public static void RemoveReservedSlot(this ReferenceHub hub)
        {
            if (!hub.HasReservedSlot())
                return;

            var lines = File.ReadAllLines(PluginAPI.Core.ReservedSlots.FilePath);
            var index = lines.FindIndex(l => l.Contains(hub.UserId()));

            if (index != -1)
            {
                lines[index] = $"# Removed by Compendium's API: {lines[index]}";
                File.WriteAllLines(PluginAPI.Core.ReservedSlots.FilePath, lines);
            }

            ReservedSlot.Reload();

            Plugin.Debug($"Removed reserved slot for {hub.GetLogName(true, false)}");
        }

        public static int PlyId(this ReferenceHub hub, int? playerId)
        {
            if (playerId.HasValue)
            {
                hub.Network_playerId = new RecyclablePlayerId(playerId.Value);
                return playerId.Value;
            }

            return hub.Network_playerId.Value;
        }

        public static uint NetId(this ReferenceHub hub)
            => hub.netId;

        public static int ObjectId(this ReferenceHub hub)
            => hub.GetInstanceID();

        public static string GetLogName(this ReferenceHub hub, bool includeIp = false, bool includeRole = true)
            => $"[{hub.PlayerId}]{(includeRole ? $" {hub.GetRoleId().ToString().SpaceByPascalCase()} " : "  ")}{hub.Nick()} {hub.UserId()}{(includeIp ? $" {hub.Ip()}" : "")}";
    }

    public static class HubModerationExtensions
    {
        public static void Kick(this ReferenceHub hub, string reason = "No reason provided.")
        {
            ServerConsole.Disconnect(hub.connectionToClient, reason);
        }

        public static void Ban(this ReferenceHub hub, bool issueIp = true, string duration = "5m", string reason = "No reason provided.")
        {
            var dur = Misc.RelativeTimeToSeconds(duration);

            if (dur > 0)
            {
                BanHandler.IssueBan(new BanDetails
                {
                    Expires = (DateTime.Now + TimeSpan.FromSeconds(dur)).Ticks,
                    Id = hub.characterClassManager.UserId,
                    IssuanceTime = DateTime.Now.Ticks,
                    Issuer = "Dedicated Server",
                    OriginalName = hub.Nick(),
                    Reason = reason
                }, BanHandler.BanType.UserId, true);

                if (issueIp)
                {
                    BanHandler.IssueBan(new BanDetails
                    {
                        Expires = (DateTime.Now + TimeSpan.FromSeconds(dur)).Ticks,
                        Id = hub.Ip(),
                        IssuanceTime = DateTime.Now.Ticks,
                        Issuer = "Dedicated Server",
                        OriginalName = hub.Nick(),
                        Reason = reason
                    }, BanHandler.BanType.IP, true);
                }
            }
        }
    }

    public static class HubStatExtensions
    {
        public static float Stamina(this ReferenceHub hub, float? newValue = null)
        {
            if (!hub.playerStats.TryGetModule<StaminaStat>(out var stamina))
                return 0f;

            if (!newValue.HasValue)
                return stamina.CurValue;

            return (stamina.CurValue = newValue.Value);
        }

        public static float HumeShield(this ReferenceHub hub, float? newValue = null)
        {
            if (!hub.playerStats.TryGetModule<HumeShieldStat>(out var hs))
                return 0f;

            if (!newValue.HasValue)
                return hs.CurValue;

            return (hs.CurValue = newValue.Value);
        }

        public static float Vigor(this ReferenceHub hub, float? newValue = null)
        {
            if (hub.Role() is Scp106Role scp106Role)
            {
                if (!scp106Role.SubroutineModule.TryGetSubroutine<Scp106VigorAbilityBase>(out var vigor))
                    return 0f;

                if (!newValue.HasValue)
                    return vigor.VigorAmount;

                return (vigor.VigorAmount = newValue.Value);
            }

            return 0f;
        }

        public static void Heal(this ReferenceHub hub, float hp)
            => hub.Health(hp + hub.Health());

        public static void Kill(this ReferenceHub hub, DeathTranslation? reason = null)
            => hub.playerStats.KillPlayer(new UniversalDamageHandler(float.MaxValue, (reason.HasValue ? reason.Value : DeathTranslations.Warhead)));

        public static void Damage(this ReferenceHub hub, float damage, DeathTranslation? reason = null)
            => hub.Damage(new UniversalDamageHandler(damage, (reason.HasValue ? reason.Value : DeathTranslations.Warhead)));

        public static void Damage(this ReferenceHub hub, DamageHandlerBase damageHandlerBase)
            => damageHandlerBase.ApplyDamage(hub);

        public static float Health(this ReferenceHub hub, float? newValue = null)
        {
            if (!hub.playerStats.TryGetModule<HealthStat>(out var hp))
                return 0f;

            if (!newValue.HasValue)
                return hp.CurValue;

            return (hp.CurValue = newValue.Value);
        }

        public static float MaxHealth(this ReferenceHub hub, float? newValue = null, bool resetOnChange = true)
        {
            if (!hub.playerStats.TryGetModule<HealthStat>(out var hp))
                return 0f;

            if (!newValue.HasValue)
                return hp.MaxValue;

            if (hp is CustomHealthStat stat)
            {
                stat.MaxHealth = newValue.Value;
                stat.ShouldReset = resetOnChange;
            }

            return hp.MaxValue;
        }

        public static bool IsGrounded(this ReferenceHub hub)
        {
            if (!(hub.Role() is IFpcRole fpcRole))
                return false;

            return fpcRole.FpcModule.IsGrounded;
        }
    }

    public static class HubRoleExtensions
    {
        public static RoleTypeId PreviousRole(this ReferenceHub hub)
        {
            if (SnapshotManager.TryGetSnapshots(hub, out var snapshots) && snapshots.Any())
            {
                var last = snapshots.LastOrDefault();

                if (last is null || !last.Data.TryGetFirst(d => d.Type is SnapshotDataType.Role, out var data) || data is null || !(data is RoleData roleData))
                    return RoleTypeId.None;

                return roleData.Role;
            }

            return RoleTypeId.None;
        }

        public static bool ToPreviousRole(this ReferenceHub hub)
        {
            if (SnapshotManager.TryGetSnapshots(hub, out var snapshots) && snapshots.Any())
            {
                var snapshot = snapshots.LastOrDefault();

                if (snapshot != null)
                {
                    snapshot.Apply(hub);
                    return true;
                }
                else
                    return false;
            }

            return false;
        }

        public static byte UnitId(this ReferenceHub hub)
            => UnitHelper.TryGetUnitId(hub, out var unitId) ? unitId : byte.MinValue;

        public static string UnitName(this ReferenceHub hub)
            => UnitHelper.TryGetUnitName(hub, out var unitName) ? unitName : null;

        public static void SetUnitId(this ReferenceHub hub, byte id)
            => UnitHelper.TrySetUnitId(hub, id);

        public static void SetUnitName(this ReferenceHub hub, string name)
            => UnitHelper.TrySetUnitName(hub, name);

        public static void SyncUnit(this ReferenceHub hub, ReferenceHub other)
        {
            if (UnitHelper.TryGetUnitId(other, out var unitId))
                hub.SetUnitId(unitId);
        }

        public static string RoleName(this ReferenceHub hub)
            => string.IsNullOrWhiteSpace(hub.Role()?.RoleName) ? hub.GetRoleId().ToString().SpaceByPascalCase() : hub.Role().RoleName;

        public static RoleTypeId RoleId(this ReferenceHub hub, RoleTypeId? newRole = null, RoleSpawnFlags flags = RoleSpawnFlags.All)
        {
            if (newRole.HasValue)
            {
                hub.roleManager.ServerSetRole(newRole.Value, RoleChangeReason.RemoteAdmin, flags);
                return newRole.Value;
            }

            return hub.GetRoleId();
        }

        public static PlayerRoleBase Role(this ReferenceHub hub, PlayerRoleBase newRole = null)
        {
            if (newRole != null)
                hub.roleManager.ServerSetRole(newRole.RoleTypeId, newRole.ServerSpawnReason, newRole.ServerSpawnFlags);

            return hub.roleManager.CurrentRole;
        }
    }

    public static class HubWorldExtensions
    {
        public static Action<ReferenceHub, string, float> HintProxy;

        public static void Broadcast(this ReferenceHub hub, object content, int time, bool clear = true)
        {
            if (clear)
                global::Broadcast.Singleton?.TargetClearElements(hub.connectionToClient);

            global::Broadcast.Singleton?.TargetAddElement(hub.connectionToClient, content.ToString(), (ushort)time, global::Broadcast.BroadcastFlags.Normal);
        }

        public static void MessageBox(this ReferenceHub hub, object content)
            => hub.gameConsoleTransmission.SendToClient($"[REPORTING] {content}", "red");

        public static void Hint(this ReferenceHub hub, object content, float duration = 5f)
            => MessageScheduler.Schedule(hub, HintMessage.Create(content?.ToString() ?? "empty", duration));

        public static void Message(this ReferenceHub hub, object content, bool isRemoteAdmin = false)
        {
            if (!isRemoteAdmin)
                hub.characterClassManager.ConsolePrint(content.ToString(), "red");
            else
                hub.queryProcessor.TargetReply(hub.connectionToClient, content.ToString(), true, false, string.Empty);
        }

        public static Vector3 Position(this ReferenceHub hub, Vector3? newPos = null, Quaternion? newRot = null)
        {
            if (newPos.HasValue)
            {
                if (newRot.HasValue)
                    hub.TryOverridePosition(newPos.Value, newRot.Value.eulerAngles);
                else
                    hub.TryOverridePosition(newPos.Value, new Vector3(32377f, 32377f, 32377f));

                return newPos.Value;
            }

            if (hub.Role() is IFpcRole fpcRole)
            {
                if (fpcRole != null && fpcRole.FpcModule != null)
                    return fpcRole.FpcModule.Position;
            }

            if (hub.Role() is Scp079Role scp079Role)
            {
                if (scp079Role.CurrentCamera != null && scp079Role.CurrentCamera.Room != null)
                    return scp079Role.CurrentCamera.Room.transform.position;
            }

            return hub.PlayerCameraReference.position;
        }

        public static RelativePosition RelativePosition(this ReferenceHub hub)
            => new RelativePosition(hub.transform.position);

        public static Quaternion Rotation(this ReferenceHub hub, Quaternion? newRot = null)
        {
            if (newRot.HasValue)
            {
                if (hub.Role() is IFpcRole fRole)
                {
                    if (fRole.FpcModule != null)
                    {
                        if (fRole.FpcModule.MouseLook != null)
                        {
                            fRole.FpcModule.MouseLook.CurrentHorizontal = newRot.Value.y;
                            fRole.FpcModule.MouseLook.CurrentVertical = newRot.Value.z;
                            fRole.FpcModule.MouseLook.ApplySyncValues((ushort)newRot.Value.y, (ushort)newRot.Value.z);
                        }
                    }
                }

                return newRot.Value;
            }

            if (hub.Role() is IFpcRole fpcRole)
            {
                if (fpcRole.FpcModule != null)
                {
                    if (fpcRole.FpcModule.MouseLook != null)
                    {
                        return new Quaternion(0f, fpcRole.FpcModule.MouseLook.CurrentHorizontal, fpcRole.FpcModule.MouseLook.CurrentVertical, 0f);
                    }
                }
            }

            if (hub.Role() is Scp079Role scp079Role)
            {
                if (scp079Role.CurrentCamera != null)
                    return new Quaternion(0f, scp079Role.CurrentCamera.HorizontalRotation, scp079Role.VerticalRotation, scp079Role.CurrentCamera.RollRotation);
            }

            return hub.PlayerCameraReference.rotation;
        }

        public static RoomIdentifier Room(this ReferenceHub hub)
            => RoomIdUtils.RoomAtPosition(hub.Position());

        public static FacilityZone Zone(this ReferenceHub hub)
        {
            var room = hub.Room();

            if (room != null)
                return room.Zone;

            return FacilityZone.None;
        }

        public static RoomName RoomId(this ReferenceHub hub)
        {
            var room = hub.Room();

            if (room != null)
                return room.Name;

            return MapGeneration.RoomName.Unnamed;
        }

        public static string RoomName(this ReferenceHub hub)
        {
            var room = hub.Room();

            if (room != null)
                return room.name;

            return "unknown room";
        }

        public static bool IsHandcuffed(this ReferenceHub hub)
            => hub.inventory.IsDisarmed();

        public static bool HasHandcuffed(this ReferenceHub hub)
            => hub.GetCuffed() != null;

        public static void Handcuff(this ReferenceHub hub, ReferenceHub cuffer = null)
            => hub.inventory.SetDisarmedStatus(cuffer?.inventory ?? ReferenceHub.HostHub.inventory);

        public static void Uncuff(this ReferenceHub hub)
            => hub.inventory.SetDisarmedStatus(null);

        public static ReferenceHub GetCuffer(this ReferenceHub hub)
            => Hub.GetHub(DisarmedPlayers.Entries.FirstOrDefault(x => x.DisarmedPlayer == hub.netId).Disarmer);

        public static ReferenceHub GetCuffed(this ReferenceHub hub)
            => Hub.GetHub(DisarmedPlayers.Entries.FirstOrDefault(x => x.Disarmer == hub.netId).DisarmedPlayer);
    }

    public static class InventoryExtensions
    {
        public static ItemType[] GetItemIds(this ReferenceHub hub)
            => hub.inventory.UserInventory.Items.Select(p => p.Value.ItemTypeId).ToArray();

        public static ItemType GetCurrentItemId(this ReferenceHub hub)
            => hub.inventory._curInstance?.ItemTypeId ?? ItemType.None;

        public static bool SetCurrentItemId(this ReferenceHub hub, ItemType item, bool useInventory = true)
        {
            if (useInventory && hub.inventory.UserInventory.Items.TryGetFirst(p => p.Value.ItemTypeId == item, out var pair) && pair.Value != null)
            {
                hub.inventory.ServerSelectItem(pair.Key);
                return hub.inventory._curInstance != null && hub.inventory._curInstance.ItemTypeId == item;
            }
            else
            {
                var itemObj = hub.AddItem(item, false);

                if (itemObj is null)
                    return false;

                hub.inventory.ServerSelectItem(itemObj.ItemSerial);

                return hub.inventory._curInstance != null && hub.inventory._curInstance.ItemSerial == itemObj.ItemSerial;
            }
        }

        public static ItemBase AddItem(this ReferenceHub hub, ItemType item, bool dropIfFull = true)
        {
            if (hub.inventory.UserInventory.Items.Count >= 8)
            {
                if (dropIfFull)
                    World.SpawnItem(item, hub.Position(), hub.Rotation());

                return null;
            }

            return hub.inventory.ServerAddItem(item);
        }

        public static ItemBase[] GetItems(this ReferenceHub hub)
            => hub.inventory.UserInventory.Items.Select(p => p.Value).ToArray();

        public static ItemBase[] GetItems(this ReferenceHub hub, ItemType itemType)
            => hub.inventory.UserInventory.Items.Where(p => p.Value.ItemTypeId == itemType).Select(p => p.Value).ToArray();

        public static void ClearItems(this ReferenceHub hub)
            => hub.GetItems().ForEach(item => hub.inventory.ServerRemoveItem(item.ItemSerial, item.PickupDropModel));

        public static ushort GetAmmo(this ReferenceHub hub, ItemType ammoType)
            => hub.inventory.UserInventory.ReserveAmmo.TryGetValue(ammoType, out var amount) ? amount : ushort.MinValue;

        public static void SetAmmo(this ReferenceHub hub, ItemType ammoType, ushort amount)
        {
            hub.inventory.UserInventory.ReserveAmmo[ammoType] = amount;
            hub.inventory.SendAmmoNextFrame = true;
        }
    }

    public static class Hub
    { 
        public static IReadOnlyList<ReferenceHub> Hubs => ReferenceHub.AllHubs.Where(hub => hub.IsPlayer()).ToList();
        public static int Count => Hubs.Count;

        public static ReferenceHub GetHub(this PlayerDataRecord record, bool supplyServer = true)
            => TryGetHub(record, out var hub) ? hub : (supplyServer ? ReferenceHub.HostHub : null);

        public static ReferenceHub GetHub(uint netId)
            => Hubs.TryGetFirst(x => x.netId == netId, out var hub) ? hub : null;

        public static bool TryGetHub(this PlayerDataRecord record, out ReferenceHub hub)
            => Hubs.TryGetFirst(h => 
                    record.Id == h.UniqueId() ||
                    record.UserId == h.UserId() ||
                    record.Ip == h.Ip(), out hub);

        public static void TryInvokeHub(this PlayerDataRecord record, Action<ReferenceHub> target)
        {
            if (record is null)
                return;

            if (record.TryGetHub(out var hub) && hub != null)
                Calls.Delegate(target, hub);
        }

        public static bool TryGetHub(string userId, out ReferenceHub hub)
            => Hubs.TryGetFirst(x => UserIdComparison.Compare(userId, x.UserId()), out hub);

        public static ReferenceHub[] InRadius(Vector3 position, float radius, FacilityZone[] zoneFilter = null, RoomName[] roomFilter = null)
        {
            var list = new List<ReferenceHub>();

            ForEach(hub =>
            {
                var room = hub.Room();

                if (zoneFilter != null && zoneFilter.Any())
                {
                    if (room is null)
                        return;

                    if (!zoneFilter.Contains(room.Zone))
                        return;
                }

                if (roomFilter != null && roomFilter.Any())
                {
                    if (room is null)
                        return;

                    if (!roomFilter.Contains(room.Name))
                        return;
                }

                if (!hub.Position().IsWithinDistance(position, radius))
                    return;

                list.Add(hub);

            });

            return list.ToArray();
        }

        public static void ForEach(this Action<ReferenceHub> action, Predicate<ReferenceHub> predicate = null)
        {
            ReferenceHub.AllHubs.ForEach(hub =>
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient)
                    return;

                if (predicate != null && !predicate(hub))
                    return;

                action(hub);
            });
        }

        public static void ForEach(this Action<ReferenceHub> action, params RoleTypeId[] roleFilter)
            => ForEach(action, hub => roleFilter.Contains(hub.GetRoleId()));

        public static void ForEachZone(this Action<ReferenceHub> action, bool includeUnknown, params FacilityZone[] zoneFilter)
            => ForEach(action, hub =>
            {
                var zone = hub.Zone();

                if (zone is FacilityZone.None && includeUnknown)
                    return true;

                return zoneFilter.Contains(zone);
            });

        public static void ForEachRoom(this Action<ReferenceHub> action, bool includeUnknown, params RoomName[] roomFilter)
            => ForEach(action, hub =>
            {
                var room = hub.RoomId();

                if (room is RoomName.Unnamed && includeUnknown)
                    return true;

                return roomFilter.Contains(room);
            });
    }

    public static class HubExtensionPatches
    {
        [Patch(typeof(FpcServerPositionDistributor), nameof(FpcServerPositionDistributor.WriteAll), PatchType.Prefix)]
        private static bool InvisPatch(ReferenceHub receiver, NetworkWriter writer)
        {
            var index = ushort.MinValue;
            var visRole = receiver.roleManager.CurrentRole as ICustomVisibilityRole;
            var hasRole = false;

            VisibilityController controller = null;

            if (visRole != null)
            {
                hasRole = true;
                controller = visRole.VisibilityController;
            }
            else
            {
                hasRole = false;
                controller = null;
            }

            ReferenceHub.AllHubs.ForEach(hub =>
            {
                if (hub.netId != receiver.netId)
                {
                    if (hub.Role() is IFpcRole fpcRole)
                    {
                        var isInvisible = hasRole && !controller.ValidateVisibility(hub);

                        if (!isInvisible)
                        {
                            if (hub.IsInvisible())
                                isInvisible = true;
                            else if (hub.IsInvisibleTo(receiver))
                                isInvisible = true;
                        }

                        var syncData = FpcServerPositionDistributor.GetNewSyncData(receiver, hub, fpcRole.FpcModule, isInvisible);

                        if (!isInvisible)
                        {
                            FpcServerPositionDistributor._bufferPlayerIDs[index] = hub.PlayerId;
                            FpcServerPositionDistributor._bufferSyncData[index] = syncData;

                            index++;
                        }
                    }
                }
            });

            writer.WriteUShort(index);

            for (int i = 0; i < index; i++)
            {
                writer.WriteRecyclablePlayerId(new RecyclablePlayerId(FpcServerPositionDistributor._bufferPlayerIDs[i]));
                FpcServerPositionDistributor._bufferSyncData[i].Write(writer);
            }

            return false;
        }

        [Patch(typeof(RaPlayerList), nameof(RaPlayerList.ReceiveData), PatchType.Prefix, typeof(CommandSender), typeof(string))]
        private static bool RaPlayerListPatch(RaPlayerList __instance, CommandSender sender, string data)
        {
            var array = data.Split(' ');

            if (array.Length != 3)
                return false;

            if (!int.TryParse(array[0], out var num) || !int.TryParse(array[1], out var num2))
                return false;

            if (!Enum.IsDefined(typeof(RaPlayerList.PlayerSorting), num2))
                return false;

            var flag = num == 1;
            var flag2 = array[2].Equals("1");
            var sortingType = (RaPlayerList.PlayerSorting)num2;
            var viewHiddenBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenBadges);
            var viewHiddenGlobalBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenGlobalBadges);
            var playerCommandSender = sender as PlayerCommandSender;

            if (playerCommandSender != null && playerCommandSender.ServerRoles.Staff)
            {
                viewHiddenBadges = true;
                viewHiddenGlobalBadges = true;
            }

            var stringBuilder = StringBuilderPool.Pool.Get();

            stringBuilder.Append("\n");

            foreach (var referenceHub in (flag2 ? __instance.SortPlayersDescending(sortingType) : __instance.SortPlayers(sortingType)))
            {
                if (referenceHub.Mode != ClientInstanceMode.ReadyClient
                    && (!referenceHub.TryGetNpc(out var npc) || !npc.ShowInPlayerList))
                    continue;

                var isInOverwatch = referenceHub.serverRoles.IsInOverwatch;
                var flag3 = VoiceChatMutes.IsMuted(referenceHub, false);
                var hasIcon = referenceHub.TryGetRaIcon(out var icon);

                stringBuilder.Append(__instance.GetPrefix(referenceHub, viewHiddenBadges, viewHiddenGlobalBadges));

                if (isInOverwatch || (hasIcon && icon == RemoteAdminIconType.Overwatch))
                    stringBuilder.Append("<link=RA_OverwatchEnabled><color=white>[</color><color=#03f8fc></color><color=white>]</color></link> ");

                if (flag3 || (hasIcon && icon == RemoteAdminIconType.Muted))
                    stringBuilder.Append("<link=RA_Muted><color=white>[</color>\ud83d\udd07<color=white>]</color></link> ");

                stringBuilder.Append("<color={RA_ClassColor}>(").Append(referenceHub.PlayerId).Append(") ");
                stringBuilder.Append(referenceHub.nicknameSync.CombinedName.Replace("\n", string.Empty).Replace("RA_", string.Empty)).Append("</color>");
                stringBuilder.AppendLine();
            }

            sender.RaReply(string.Format("${0} {1}", __instance.DataId, StringBuilderPool.Pool.PushReturn(stringBuilder)), true, !flag, string.Empty);
            return false;
        }

        [Patch(typeof(FpcServerPositionDistributor), nameof(FpcServerPositionDistributor.GetNewSyncData), PatchType.Prefix)]
        public static bool GenerateNewSyncData(ReferenceHub receiver, ReferenceHub target, FirstPersonMovementModule fpmm, bool isInvisible, ref FpcSyncData __result)
        {
            var position = Vector3.zero;

            if (!target.TryGetFakePosition(target, out position))
                position = target.transform.position;

            var prevSyncData = FpcServerPositionDistributor.GetPrevSyncData(receiver, target);
            var fpcSyncData = isInvisible ? default : new FpcSyncData(prevSyncData, fpmm.SyncMovementState, fpmm.IsGrounded, new RelativePosition(position), fpmm.MouseLook);
            
            FpcServerPositionDistributor.PreviouslySent[receiver.netId][target.netId] = fpcSyncData;

            __result = fpcSyncData;
            return false;
        }

        [Patch(typeof(PlayerEffectsController), nameof(PlayerEffectsController.ServerSyncEffect), PatchType.Prefix)]
        public static bool SyncEffectIntensity(PlayerEffectsController __instance, StatusEffectBase effect)
        {
            for (int i = 0; i < __instance.EffectsLength; i++)
            {
                var statusEffectBase = __instance.AllEffects[i];

                if (statusEffectBase == effect)
                {
                    if (__instance._hub.TryGetFakeIntensity(statusEffectBase.GetType(), out var intensity))
                        __instance._syncEffectsIntensity[i] = intensity;
                    else
                        __instance._syncEffectsIntensity[i] = statusEffectBase.Intensity;

                    return false;
                }
            }

            return false;
        }
    }
}
