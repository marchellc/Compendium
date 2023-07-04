using Compendium.Extensions;
using Compendium.Features;
using Compendium.Helpers.Calls;
using Compendium.Helpers.Events;
using Compendium.Helpers.Overlay;

using helpers.Configuration.Ini;
using helpers.Extensions;
using helpers.Random;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Pickups;

using MapGeneration;

using PlayerRoles;

using PluginAPI.Enums;
using PluginAPI.Events;

using Scp914;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Scp914
{
    public static class Scp914Logic
    {
        [IniConfig("Teleport Allowed Roles", null, "Roles allowed to use the teleport.")]
        public static List<RoleTypeId> AllowTeleportRoles { get; set; } = new List<RoleTypeId>()
        {
            RoleTypeId.ChaosRifleman,
            RoleTypeId.ChaosMarauder,
            RoleTypeId.ChaosRepressor,
            RoleTypeId.ChaosConscript,

            RoleTypeId.NtfCaptain,
            RoleTypeId.NtfSergeant,
            RoleTypeId.NtfPrivate,
            RoleTypeId.NtfSpecialist,

            RoleTypeId.ClassD,
            RoleTypeId.Scientist,
            RoleTypeId.CustomRole,
            RoleTypeId.Tutorial
        };

        [IniConfig("Teleport Chances", null, "Teleport chance per knob setting.")]
        public static Dictionary<Scp914KnobSetting, int> TeleportChances { get; set; } = new Dictionary<Scp914KnobSetting, int>()
        {
            [Scp914KnobSetting.Rough] = 5,
            [Scp914KnobSetting.Coarse] = 15,
            [Scp914KnobSetting.OneToOne] = 20,
            [Scp914KnobSetting.Fine] = 25,
            [Scp914KnobSetting.VeryFine] = 35
        };

        [IniConfig("Teleport Distances", null, "Teleport distance per knob setting.")]
        public static Dictionary<Scp914KnobSetting, float> TeleportDistances { get; set; } = new Dictionary<Scp914KnobSetting, float>()
        {
            [Scp914KnobSetting.Rough] = 30f,
            [Scp914KnobSetting.Coarse] = 50f,
            [Scp914KnobSetting.OneToOne] = 70f,
            [Scp914KnobSetting.Fine] = 80f,
            [Scp914KnobSetting.VeryFine] = 100f
        };

        [IniConfig("Recipes", null, "A list of SCP-914 recipes.")]
        public static Dictionary<Scp914KnobSetting, Dictionary<ItemType, Dictionary<int, ItemType>>> Recipes { get; set; } = new Dictionary<Scp914KnobSetting, Dictionary<ItemType, Dictionary<int, ItemType>>>()
        {
            [Scp914KnobSetting.Rough] = new Dictionary<ItemType, Dictionary<int, ItemType>>() { [ItemType.None] = new Dictionary<int, ItemType>() { [0] = ItemType.None } },
            [Scp914KnobSetting.Coarse] = new Dictionary<ItemType, Dictionary<int, ItemType>>() { [ItemType.None] = new Dictionary<int, ItemType>() { [0] = ItemType.None } },
            [Scp914KnobSetting.OneToOne] = new Dictionary<ItemType, Dictionary<int, ItemType>>() { [ItemType.None] = new Dictionary<int, ItemType>() { [0] = ItemType.None } },
            [Scp914KnobSetting.Fine] = new Dictionary<ItemType, Dictionary<int, ItemType>>() { [ItemType.None] = new Dictionary<int, ItemType>() { [0] = ItemType.None } },
            [Scp914KnobSetting.VeryFine] = new Dictionary<ItemType, Dictionary<int, ItemType>>() { [ItemType.None] = new Dictionary<int, ItemType>() { [0] = ItemType.None } }
        };

        [IniConfig("Effects", null, "A list of SCP-914 effects.")]
        public static Dictionary<Scp914KnobSetting, Dictionary<RoleTypeId, Dictionary<int, Scp914Effect>>> Effects { get; set; } = new Dictionary<Scp914KnobSetting, Dictionary<RoleTypeId, Dictionary<int, Scp914Effect>>>()
        {
            [Scp914KnobSetting.Rough] = new Dictionary<RoleTypeId, Dictionary<int, Scp914Effect>>() { [RoleTypeId.None] = new Dictionary<int, Scp914Effect>() { [0] = new Scp914Effect() } },
            [Scp914KnobSetting.Coarse] = new Dictionary<RoleTypeId, Dictionary<int, Scp914Effect>>() { [RoleTypeId.None] = new Dictionary<int, Scp914Effect>() { [0] = new Scp914Effect() } },
            [Scp914KnobSetting.OneToOne] = new Dictionary<RoleTypeId, Dictionary<int, Scp914Effect>>() { [RoleTypeId.None] = new Dictionary<int, Scp914Effect>() { [0] = new Scp914Effect() } },
            [Scp914KnobSetting.Fine] = new Dictionary<RoleTypeId, Dictionary<int, Scp914Effect>>() { [RoleTypeId.None] = new Dictionary<int, Scp914Effect>() { [0] = new Scp914Effect() } },
            [Scp914KnobSetting.VeryFine] = new Dictionary<RoleTypeId, Dictionary<int, Scp914Effect>>() { [RoleTypeId.None] = new Dictionary<int, Scp914Effect>() { [0] = new Scp914Effect() } }
        };

        [IniConfig("Fix Room Positions", null, "Whether or not to add to the Y axis when teleporting a player to a room.")]
        public static bool FixRoomPositions { get; set; } = true;

        [IniConfig("Fix Player Positions", null, "Whether or not to add to the Y axis when teleport a player to another player.")]
        public static bool FixPlayerPositions { get; set; } = true;

        public static Scp914Controller Controller => Scp914Controller.Singleton;

        public static void Load()
        {
            ServerEventType.Scp914ProcessPlayer.AddHandler<Action<Scp914ProcessPlayerEvent>>(OnPlayerUpgraded);
            ServerEventType.Scp914UpgradePickup.AddHandler<Action<Scp914UpgradePickupEvent, ValueContainer>>(OnUpgradingPickup);
            ServerEventType.Scp914UpgradeInventory.AddHandler<Action<Scp914UpgradeInventoryEvent, ValueContainer>>(OnUpgradingInventory);

            FLog.Info("Event handlers registered.");
        }

        public static void Unload()
        {
            ServerEventType.Scp914ProcessPlayer.RemoveHandler<Action<Scp914ProcessPlayerEvent>>(OnPlayerUpgraded);
            ServerEventType.Scp914UpgradePickup.RemoveHandler<Action<Scp914UpgradePickupEvent, ValueContainer>>(OnUpgradingPickup);
            ServerEventType.Scp914UpgradeInventory.RemoveHandler<Action<Scp914UpgradeInventoryEvent, ValueContainer>>(OnUpgradingInventory);

            FLog.Warn("Event handlers unregistered.");
        } 

        private static void OnUpgradingInventory(Scp914UpgradeInventoryEvent ev, ValueContainer result)
        {
            FLog.Debug($"Upgrading inventory player={ev.Player.ReferenceHub.LoggedNameFromRefHub()} role={ev.Player.Role} knob={ev.KnobSetting} item={ev.Item.ItemTypeId}");

            if (Recipes.TryGetValue(ev.KnobSetting, out var knobRecipes))
            {
                FLog.Debug($"Found {knobRecipes.Count} recipes for knob");

                if (knobRecipes.TryGetValue(ev.Item.ItemTypeId, out var recipes))
                {
                    FLog.Debug($"Found {recipes.Count} for item");

                    var recipe = WeightedRandomGeneration.Default.PickObject(r => r.Key, recipes.ToArray());

                    FLog.Debug($"Selected recipe: {recipe.Value} ({recipe.Key})");

                    if (recipe.Key > 0)
                    {
                        result.Value = false;
                        FLog.Debug($"Disabled return");

                        if (recipe.Value == ev.Item.ItemTypeId)
                        {
                            FLog.Debug($"Item is the same type");

                            if (ev.Item is IUpgradeTrigger upgradeTrigger && upgradeTrigger != null)
                            {
                                upgradeTrigger.ServerOnUpgraded(ev.KnobSetting);
                                FLog.Debug($"Fired trigger");
                            }

                            return;
                        }

                        var itemId = ev.Item.ItemTypeId;

                        ev.Player.ReferenceHub.inventory.ServerRemoveItem(ev.Item.ItemSerial, null);
                        FLog.Debug($"Removed item");

                        if (recipe.Value is ItemType.None)
                        {
                            ev.Player.ReferenceHub.ShowMessage($"\n\n<b>Bad luck, mate.\n<i>(item removed: {itemId.ToString().SpaceByPascalCase()}</i>");
                            FLog.Debug($"Return - Item removed - none result");
                            return;
                        }

                        if (!InventoryItemLoader.AvailableItems.ContainsKey(recipe.Value))
                        {
                            FLog.Debug($"Return - no available items");
                            return;
                        }

                        var item = ev.Player.ReferenceHub.inventory.ServerAddItem(recipe.Value);

                        FLog.Debug($"Added item: {item != null}");

                        if (item is Firearm firearm)
                        {
                            FLog.Debug($"Item is firearm");

                            var status = SetupFirearm(item.ItemTypeId);

                            if (status.HasValue)
                                firearm.Status = status.Value;
                            else
                                FLog.Debug($"Failed to setup firearm");
                        }
                        else
                        {
                            FLog.Debug($"Item is not a firearm");
                        }

                        FLog.Debug($"Finished upgrading");
                    }
                    else
                    {
                        FLog.Debug($"Recipe is invalid");
                    }
                }
            }
            else
            {
                FLog.Debug($"No recipes found.");
            }
        }

        private static void OnUpgradingPickup(Scp914UpgradePickupEvent ev, ValueContainer result)
        {
            FLog.Debug($"Upgrading pickup item={ev.Item.Info.ItemId}");

            if (Recipes.TryGetValue(ev.KnobSetting, out var recipes))
            {
                FLog.Debug($"Found {recipes.Count} knob recipes");

                if (recipes.TryGetValue(ev.Item.NetworkInfo.ItemId, out var available))
                {
                    FLog.Debug($"Found {available.Count} item recipes");

                    if (available.Any())
                    {
                        var target = WeightedRandomGeneration.Default.PickObject(pair => pair.Key, available.ToArray());

                        FLog.Debug($"Selected recipe: {target.Value} ({target.Key})");

                        if (target.Key > 0)
                        {
                            result.Value = false;
                            FLog.Debug($"Event disallowed");

                            if (target.Value is ItemType.None)
                            {
                                FLog.Debug($"Destroying item");

                                if (ev.Item.PreviousOwner.Hub != null)
                                    ev.Item.PreviousOwner.Hub.ShowMessage($"\n\n<b>Looks like luck isn't on your side today ..<b>\n<i>(item destroyed: {ev.Item.Info.ItemId.ToString().SpaceByPascalCase()})</i>", 3f, true);

                                ev.Item.DestroySelf();
                                FLog.Debug($"Return - Item destroyed - result is none");
                                return;
                            }

                            if (target.Value == ev.Item.Info.ItemId ||
                                !InventoryItemLoader.AvailableItems.TryGetValue(ev.Item.Info.ItemId, out var item) ||
                                item is null)
                            {
                                FLog.Debug($"Teleporting original item");

                                ev.Item.transform.position = ev.OutputPosition;

                                if (ev.Item is IUpgradeTrigger upgradeTrigger)
                                {
                                    upgradeTrigger.ServerOnUpgraded(ev.KnobSetting);
                                    FLog.Debug($"Fired upgrade trigger");
                                }

                                FLog.Debug($"Return - Item is the same type");
                                return;
                            }

                            var info = new PickupSyncInfo
                            {
                                ItemId = item.ItemTypeId,
                                WeightKg = item.Weight,
                                Serial = ItemSerialGenerator.GenerateNext()
                            };

                            FLog.Debug($"Generated new info");

                            _ = InventoryExtensions.ServerCreatePickup(item, info, ev.OutputPosition, true, p =>
                            {
                                if (p is FirearmPickup firearm)
                                {
                                    FLog.Debug($"Setting up firearm");

                                    var status = SetupFirearm(target.Value);

                                    if (status.HasValue)
                                        firearm.NetworkStatus = status.Value;
                                }
                            });

                            FLog.Debug($"Spawned pickup");

                            ev.Item.DestroySelf();

                            FLog.Debug($"Destroyed original item");
                        }
                    }
                    else
                    {
                        FLog.Debug($"No available item recipes");
                    }
                }
                else
                {
                    FLog.Debug($"No available knob recipes");
                }
            }
        }

        private static void OnPlayerUpgraded(Scp914ProcessPlayerEvent ev)
        {
            CallHelper.CallWhenFalse(() =>
            {
                FLog.Debug($"Upgrading player={ev.Player.ReferenceHub.LoggedNameFromRefHub()} role={ev.Player.Role}");

                if (Effects.TryGetValue(ev.KnobSetting, out var effectList))
                {
                    FLog.Debug($"Found {effectList.Count} effects for knob");

                    if (effectList.TryGetValue(ev.Player.Role, out var effects))
                    {
                        FLog.Debug($"Found {effects.Count} effects for role");

                        if (effects.Any() && effects.Sum(ef => ef.Key) == 100)
                        {
                            var effect = WeightedRandomGeneration.Default.PickObject(ef => ef.Key, effects.ToArray());

                            FLog.Debug($"Selected effect: {effect.Value?.Effect ?? "invalid"} ({effect.Key})");

                            if (effect.Value != null && effect.Key > 0 && effect.Value.Effect != "default")
                            {
                                FLog.Debug($"Proceeding");

                                var host = ReferenceHub.HostHub;

                                if (host != null)
                                {
                                    FLog.Debug($"Host is not null");

                                    if (host.playerEffectsController != null)
                                    {
                                        FLog.Debug($"Host PFX is not null");

                                        if (host.playerEffectsController._effectsByType.TryGetFirst(pair => pair.Key.Name == effect.Value.Effect, out var effectPair))
                                        {
                                            if (effectPair.Key != null)
                                            {
                                                FLog.Debug($"Found effect type in host's dictionary: {effectPair.Key.FullName}");
                                                var targetEffect = ev.Player.ReferenceHub.playerEffectsController.AllEffects.FirstOrDefault(ef => ef.GetType() == effectPair.Key);

                                                FLog.Debug($"Found target effect: {targetEffect != null}");

                                                if (targetEffect != null)
                                                {
                                                    targetEffect.ServerSetState(effect.Value.Intensity, effect.Value.Duration, effect.Value.AddDuration);
                                                    FLog.Debug($"Activated target effect intensity={effect.Value.Intensity} duration={effect.Value.Duration} addDuration={effect.Value.AddDuration}");
                                                }
                                            }
                                            else
                                            {
                                                FLog.Debug($"effectPair key is null");
                                            }
                                        }
                                        else
                                        {
                                            FLog.Debug($"Failed to find effect by name {effect.Value.Effect} in host's dictionary");
                                        }
                                    }
                                    else
                                    {
                                        FLog.Debug($"Host PFX is null");
                                    }
                                }
                                else
                                {
                                    FLog.Debug($"Host is null");
                                }
                            }
                        }
                    }
                }

                FLog.Debug($"Checking teleports");

                if (AllowTeleportRoles.Contains(ev.Player.Role))
                {
                    FLog.Debug($"Role is allowed");

                    if (TeleportChances.TryGetValue(ev.KnobSetting, out var chance) && chance > 0
                        && TeleportDistances.TryGetValue(ev.KnobSetting, out var distance) && distance > 0f)
                    {
                        FLog.Debug($"chance={chance} distance={distance}");

                        var isPositive = WeightedRandomGeneration.Default.GetBool(chance);

                        FLog.Debug($"isPositive={isPositive}");

                        if (isPositive)
                        {
                            var room = GetTargetRoom(distance);

                            FLog.Debug($"Found room: {room?.Name ?? RoomName.Unnamed}");

                            if (room != null)
                            {
                                var pos = room.ApiRoom.Position;

                                FLog.Debug($"Position: {pos}");

                                if (FixRoomPositions)
                                    pos.y += 1.5f;

                                FLog.Debug($"Current position: {pos}");

                                ev.Player.Position = pos;
                                ev.Player.ReferenceHub.ShowMessage($"\n\n<b>Seems like gambling can save you!</b>\n<i>(Room teleport: {room.Name} [{room.Zone}])</i>");

                                FLog.Debug($"Teleported");
                            }
                        }
                        else
                        {
                            var scp = GetTargetScp(distance);

                            FLog.Debug($"Found scp: {scp.LoggedNameFromRefHub()} ({scp.GetRoleId()})");

                            if (scp != null)
                            {
                                var pos = scp.PlayerCameraReference.position;

                                FLog.Debug($"Scp pos: {pos}");

                                if (FixPlayerPositions)
                                    pos.y += 0.5f;

                                FLog.Debug($"Current pos: {pos}");

                                ev.Player.Position = pos;
                                ev.Player.ReferenceHub.ShowMessage($"\n\n<b>That's quite the situation you got yourself into ..</b>\n<i>(SCP teleport: {scp.nicknameSync.MyNick} [{scp.roleManager.CurrentRole.RoleName}])</i>", 3f, true);

                                FLog.Debug($"Teleported to SCP");
                            }
                        }
                    }
                    else
                    {
                        FLog.Debug($"Invalid chance or distance");
                    }
                }
                else
                {
                    FLog.Debug($"No available teleports");
                }
            }, () => Controller._isUpgrading);
        }

        private static ReferenceHub GetTargetScp(float distance)
        {
            var scps = ReferenceHub.AllHubs.Where(hub => hub.Mode is ClientInstanceMode.ReadyClient && hub.IsSCP(false) && hub.IsWithinDistance(Controller, distance));

            if (scps.Any())
            {
                scps = scps.OrderByDescending(hub => hub.DistanceSquared(Controller));
                return scps.First();
            }

            return null;
        }

        private static RoomIdentifier GetTargetRoom(float distance)
        {
            var rooms = RoomIdentifier.AllRoomIdentifiers.Where(room => room.IsWithinDistance(Controller, distance));

            if (rooms.Any())
            {
                rooms = rooms.OrderByDescending(room => room.DistanceSquared(Controller));
                return rooms.First();
            }

            return null;
        }

        private static FirearmStatus? SetupFirearm(ItemType newType)
        {
            if (!InventoryItemLoader.TryGetItem<Firearm>(newType, out var firearm))
            {
                return null;
            }

            byte ammo = default;
            uint attachments = default;

            FirearmStatusFlags flags = default;

            if (firearm is ParticleDisruptor)
            {
                ammo = 5;
                flags = FirearmStatusFlags.MagazineInserted;
                attachments = firearm.ValidateAttachmentsCode(0);
            }
            else
            {
                ammo = 0;
                flags = FirearmStatusFlags.None;
                attachments = AttachmentsUtils.GetRandomAttachmentsCode(newType);
            }

            return new FirearmStatus(ammo, flags, attachments);
        }
    }
}