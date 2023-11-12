using BetterCommands;

using Compendium.Events;
using Compendium.IO.Saving;

using GameCore;

using helpers.Attributes;
using helpers.Events;
using helpers;
using helpers.Extensions;

using PluginAPI.Core;
using PluginAPI.Events;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Compendium.Input
{
    public static class InputManager
    {
        private static readonly HashSet<IInputHandler> _handlers = new HashSet<IInputHandler>();
        private static SaveFile<CollectionSaveData<InputBinding>> _binds;

        public const string SyncCmdBindingKey = "enable_sync_command_binding";

        public static readonly EventProvider OnKeyPressed = new EventProvider();
        public static readonly EventProvider OnKeyRegistered = new EventProvider();
        public static readonly EventProvider OnKeyUnregistered = new EventProvider();
        public static readonly EventProvider OnKeySynchronized = new EventProvider();

        public static bool IsEnabled { get; private set; }

        public static void Register<THandler>() where THandler : IInputHandler, new()
        {
            if (TryGetHandler<THandler>(out _))
            {
                Plugin.Warn($"Attempted to register an already existing input handler.");
                return;
            }

            _handlers.Add(new THandler());
        }

        public static void Unregister<THandler>() where THandler : IInputHandler, new()
        {
            if (_handlers.RemoveWhere(h => h is THandler) > 0)
                OnKeyUnregistered.Invoke(typeof(THandler));
        }

        public static bool TryGetHandler(string actionId, out IInputHandler handler)
            => _handlers.TryGetFirst(h => h.Id == actionId, out handler);

        public static bool TryGetHandler<THandler>(out THandler handler) where THandler : IInputHandler, new()
        {
            if (_handlers.TryGetFirst(h => h is THandler, out var result) && result is THandler castHandler)
            {
                handler = castHandler;
                return true;
            }

            handler = default;
            return false;
        }

        public static KeyCode KeyFor(ReferenceHub hub, IInputHandler handler)
        {
            if (_binds is null)
                return handler.Key;

            if (_binds.Data.TryGetFirst<InputBinding>(bind => bind.Id == handler.Id && bind.OwnerId == hub.UniqueId(), out var binding))
                return binding.Key;

            return handler.Key;
        }

        [Load]
        [Reload]
        private static void Initialize()
        {
            IsEnabled = ConfigFile.ServerConfig.GetBool(SyncCmdBindingKey);

            if (!IsEnabled)
            {
                if (_binds != null)
                {
                    _binds.Save();
                    _binds = null;
                }

                Plugin.Warn($"Synchronized binding is disabled. (set \"{SyncCmdBindingKey}\" to true in the gameplay config to enable)");
                return;
            }

            if (_binds is null)
                _binds = new SaveFile<CollectionSaveData<InputBinding>>(Directories.GetDataPath("SavedPlayerBinds", "playerBinds"));
            else
                _binds.Load();
        }

        private static void SyncPlayer(ReferenceHub hub)
        {
            _handlers.ForEach(handler =>
            {
                var key = KeyFor(hub, handler);

                hub.characterClassManager.TargetChangeCmdBinding(key, $".input {handler.Id}");
                hub.Message($"[INPUT - DEBUG] Synchronized key bind: {handler.Id} on key {key}");

                OnKeySynchronized.Invoke(hub, handler, key);
            });
        }

        private static void ReceiveKey(ReferenceHub player, string actionId)
        {
            OnKeyPressed.Invoke(player, actionId);

            if (_handlers.TryGetFirst(h => h.Id == actionId, out var handler))
            {
                try
                {
                    handler.OnPressed(player);
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed to execute key bind: {actionId}");
                    Plugin.Error(ex);

                    player.Message($"Failed to execute key bind: {actionId}");
                    player.Message(ex.Message);
                }
            }
        }

        [Command("input", CommandType.PlayerConsole)]
        [Description("Executes a key bind on the server.")]
        private static string OnInputCommand(Player sender, string actionId)
        {
            if (!IsEnabled)
                return "Key binds are disabled on this server.";

            ReceiveKey(sender.ReferenceHub, actionId);
            return "Keybind executed.";
        }

        [Command("inputsync", CommandType.PlayerConsole)]
        [Description("Synchronizes server-side keybinds.")]
        private static string OnSyncCommand(Player sender)
        {
            SyncPlayer(sender.ReferenceHub);
            return "Synchronized keybinds.";
        }

        [Command("rebind", CommandType.PlayerConsole)]
        [Description("Allows you to customize your key binds.")]
        private static string OnRebindCommand(ReferenceHub sender, string actionId, KeyCode newKey)
        {
            if (_binds.Data.TryGetFirst<InputBinding>(bind => bind.Id == actionId && bind.OwnerId == sender.UniqueId(), out var binding))
            {
                binding.Key = newKey;

                _binds.Save();

                SyncPlayer(sender);

                return $"Bind action {actionId} to key {newKey.ToString().SpaceByPascalCase()}!";
            }
            else
            {
                _binds.Data.Add(new InputBinding
                {
                    Id = actionId,
                    Key = newKey,
                    OwnerId = sender.UniqueId()
                });

                _binds.Save();

                SyncPlayer(sender);

                return $"Bind action {actionId} to key {newKey.ToString().SpaceByPascalCase()}!";
            }
        }

        [Event]
        private static void OnJoined(PlayerJoinedEvent ev)
            => SyncPlayer(ev.Player.ReferenceHub);
    }
}