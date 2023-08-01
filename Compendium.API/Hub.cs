using Compendium.Extensions;
using Compendium.PlayerData;
using Compendium.Health;
using Compendium.Hints;
using Compendium.RoleHistory;
using Compendium.Units;
using Compendium.UserId;

using helpers;
using helpers.Extensions;

using Hints;

using MapGeneration;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp106;

using PlayerStatsSystem;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

namespace Compendium
{
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

        public static string UserId(this ReferenceHub hub)
            => hub.characterClassManager.UserId;

        public static string UniqueId(this ReferenceHub hub)
            => PlayerDataRecorder.GetData(hub)?.Id ?? "";

        public static UserIdValue ParsedUserId(this ReferenceHub hub)
            => UserIdHelper.TryParse(hub.UserId(), out var parsed) ? parsed : throw new Exception($"Failed to parse user ID of {hub.UserId()}");

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
            if (Plugin.Config.ApiSetttings.IpCompatibilityMode)
                return PlayerDataRecorder.GetToken(hub)?.Ip ?? hub.connectionToClient.address;

            return hub.connectionToClient.address;
        }

        public static string GroupKey(this ReferenceHub hub, string roleKey = null)
        {
            if (ServerStatic.PermissionsHandler is null)
                return "";

            if (!string.IsNullOrWhiteSpace(roleKey))
            {
                if (!ServerStatic.PermissionsHandler._groups.TryGetValue(roleKey, out _))
                {
                    Plugin.Warn($"Tried setting role of {hub.GetLogName()} to a missing group!");
                    return "";
                }

                ServerStatic.PermissionsHandler._members[hub.characterClassManager.UserId] = roleKey;
                hub.serverRoles.RefreshPermissions();
                return roleKey;
            }

            if (ServerStatic.PermissionsHandler._members.ContainsKey(hub.characterClassManager.UserId))
                return ServerStatic.PermissionsHandler._members[hub.characterClassManager.UserId];

            return "";
        }

        public static bool HasGroupKey(this ReferenceHub hub)
            => !string.IsNullOrWhiteSpace(hub.GroupKey());

        public static bool HasReservedSlot(this ReferenceHub hub, bool countBypass = true)
            => ReservedSlot.HasReservedSlot(hub.UserId(), out var bypass) || countBypass && bypass;

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

            File.AppendAllLines(PluginAPI.Core.ReservedSlots.FilePath, new string[] { hub.UserId() });
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
            => $"[{hub.PlayerId}]:{(includeRole ? $" {hub.GetRoleId().ToString().SpaceByPascalCase()} " : "  ")}{hub.Nick()} ({hub.UserId()}){(includeIp ? $" <{hub.Ip()}>" : "")}";
    }

    public static class HubModerationExtensions
    {
        public static void Kick(this ReferenceHub hub, string reason = "No reason provided.")
        {
            if (Plugin.Config.ApiSetttings.LogKick)
            {
                var caller = Reflection.GetExecutingMethod(1);
                var callerName = $"{caller.DeclaringType.FullName} -> {caller.Name}";

                Plugin.Warn($"{hub.GetLogName(true, true)} was kicked by \"{callerName}\" with reason \"{reason}\"");
            }

            ServerConsole.Disconnect(hub.connectionToClient, reason);
        }

        public static void Ban(this ReferenceHub hub, bool issueIp = true, string duration = "5m", string reason = "No reason provided.")
        {
            var dur = Misc.RelativeTimeToSeconds(duration);

            if (dur > 0)
            {
                if (Plugin.Config.ApiSetttings.LogBan)
                {
                    var caller = Reflection.GetExecutingMethod(1);
                    var callerName = $"{caller.DeclaringType.FullName} -> {caller.Name}";

                    Plugin.Warn($"{hub.GetLogName(true, true)} was banned by \"{callerName}\" with reason \"{reason}\" for {duration} ({dur} seconds) [IP: {issueIp}]");
                }

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
                if (!scp106Role.SubroutineModule.TryGetSubroutine<Scp106Vigor>(out var vigor))
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

        public static float MaxHealth(this ReferenceHub hub, float? newValue = null)
        {
            if (!hub.playerStats.TryGetModule<HealthStat>(out var hp))
                return 0f;

            if (!newValue.HasValue)
                return hp.MaxValue;

            if (hp is CustomHealthStat stat)
                return (stat.CustomMaxValue = newValue.Value);

            return hp.MaxValue;
        }
    }

    public static class HubRoleExtensions
    {
        public static RoleHistoryEntry PreviousRole(this ReferenceHub hub)
            => RoleHistoryRecorder.TryGetPreviousRole(hub, out var role) ? role : null;

        public static bool ToPreviousRole(this ReferenceHub hub)
        {
            if (RoleHistoryRecorder.TryGetPreviousRole(hub, out var prevRole))
            {
                prevRole.Snapshot.Role.Apply(hub);
                return true;
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
            {
                hub.roleManager.ServerSetRole(newRole.RoleTypeId, newRole.ServerSpawnReason, newRole.ServerSpawnFlags);
                return hub.roleManager.CurrentRole;
            }

            return hub.roleManager.CurrentRole;
        }
    }

    public static class HubWorldExtensions
    {
        public static void Broadcast(this ReferenceHub hub, object content, int time, bool clear = true)
        {
            if (clear)
                global::Broadcast.Singleton?.TargetClearElements(hub.connectionToClient);

            global::Broadcast.Singleton?.TargetAddElement(hub.connectionToClient, content.ToString(), (ushort)time, global::Broadcast.BroadcastFlags.Normal);
        }

        public static void Hint(this ReferenceHub hub, object content, float duration, bool clear, params BasicHintEffectType[] effects)
        {
            if (clear)
                hub.hints.Show(new TextHint("", new HintParameter[] { new StringHintParameter("") }, null, 0.1f));

            if (effects.Any())
            {
                var effectList = new List<HintEffect>();

                foreach (var effect in effects)
                {
                    if (effect is BasicHintEffectType.FadeIn)
                        effectList.Add(HintEffectPresets.FadeIn());

                    if (effect is BasicHintEffectType.FadeOut)
                        effectList.Add(HintEffectPresets.FadeOut());

                    if (effect is BasicHintEffectType.Pulse)
                        effectList.Add(HintEffectPresets.PulseAlpha(5f, 7f));

                    if (effect is BasicHintEffectType.FadeInAndOut)
                        effectList.AddRange(HintEffectPresets.FadeInAndOut(duration));
                }

                hub.hints.Show(new TextHint(content.ToString(), new HintParameter[] { new StringHintParameter(content.ToString()) }, effectList.ToArray(), duration));
            }
            else
            {
                hub.hints.Show(new TextHint(content.ToString(), new HintParameter[] { new StringHintParameter(content.ToString()) }, null, duration));
            }
        }

        public static void Message(this ReferenceHub hub, object content, bool isRemoteAdmin = false)
        {
            if (!isRemoteAdmin)
                hub.characterClassManager.ConsolePrint(content.ToString(), "red");
            else
                hub.queryProcessor.TargetReply(hub.connectionToClient, $"SYS#{content}", true, false, string.Empty);
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

            return RoomName.Unnamed;
        }

        public static string RoomIdName(this ReferenceHub hub)
        {
            var room = hub.Room();

            if (room != null)
                return room.name;

            return "unknown room";
        }
    }

    public static class Hub
    { 
        public static IReadOnlyList<ReferenceHub> Hubs => ReferenceHub.AllHubs.Where(hub => hub.IsPlayer()).ToList();
        public static int Count => Hubs.Count;

        public static ReferenceHub[] InRadius(Vector3 position, float radius, FacilityZone[] zoneFilter = null, RoomName[] roomFilter = null)
        {
            var list = new List<ReferenceHub>();

            ForEach(hub =>
            {
                var room = hub.Room();

                if (zoneFilter != null)
                {
                    if (room is null)
                        return;

                    if (!zoneFilter.Contains(room.Zone))
                        return;
                }

                if (roomFilter != null)
                {
                    if (room is null)
                        return;

                    if (!roomFilter.Contains(room.Name))
                        return;
                }

                if (!hub.Position().IsWithinDistance(position, radius))
                    return;

                list.Add(hub);

            }, false);

            return list.ToArray();
        }

        public static void ForEach(this Action<ReferenceHub> action, bool includeServer = false, Predicate<ReferenceHub> predicate = null)
        {
            ReferenceHub.AllHubs.ForEach(hub =>
            {
                if (hub.Mode != ClientInstanceMode.ReadyClient && !includeServer)
                    return;

                if (predicate != null && !predicate(hub))
                    return;

                action(hub);
            });
        }

        public static void ForEach(this Action<ReferenceHub> action, params RoleTypeId[] roleFilter)
            => ForEach(action, false, hub => roleFilter.Contains(hub.GetRoleId()));

        public static void ForEach(this Action<ReferenceHub> action, bool includeUnknown, params FacilityZone[] zoneFilter)
            => ForEach(action, false, hub =>
            {
                var zone = hub.Zone();

                if (zone is FacilityZone.None && includeUnknown)
                    return true;

                return zoneFilter.Contains(zone);
            });

        public static void ForEach(this Action<ReferenceHub> action, bool includeUnknown, params RoomName[] roomFilter)
            => ForEach(action, false, hub =>
            {
                var room = hub.RoomId();

                if (room is RoomName.Unnamed && includeUnknown)
                    return true;

                return roomFilter.Contains(room);
            });
    }
}
