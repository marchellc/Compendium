using BetterCommands;

using Compendium.Features;

using helpers.Extensions;
using helpers.IO.Storage;

using PluginAPI.Core;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Compendium.Input
{
    public static class InputHandler
    {
        private static readonly HashSet<InputHandlerData> m_Inputs = new HashSet<InputHandlerData>();
        private static SingleFileStorage<InputCache> m_PlayerBinds;

        public static void Load()
        {
            m_PlayerBinds = new SingleFileStorage<InputCache>($"{FeatureManager.DirectoryPath}/input_cache");
            m_PlayerBinds.Load();
        }

        public static void Reload()
        {
            m_PlayerBinds?.Reload();
        }

        public static void Unload()
        {
            m_PlayerBinds?.Save();
            m_PlayerBinds = null;
        }

        public static bool TryReplaceKey(string actionId, KeyCode newKey, string ownerId)
        {
            if (m_PlayerBinds.Data.TryGetFirst(data => data.OwnerId == ownerId && data.Key == newKey, out var cache))
                cache.ActionId = actionId;
            else if (m_PlayerBinds.Data.TryGetFirst(data => data.OwnerId == ownerId && data.ActionId == actionId, out cache))
                cache.Key = newKey;
            else
                m_PlayerBinds.Add(new InputCache() { ActionId = actionId, Key = newKey, OwnerId = ownerId });

            m_PlayerBinds.Save();
            return true;
        }

        public static bool TryGetUserKey(string actionId, string userId, out KeyCode key)
        {
            if (m_PlayerBinds.Data.TryGetFirst(data => data.OwnerId == userId && data.ActionId == actionId, out var bind))
            {
                key = bind.Key;
                return true;
            }

            if (m_Inputs.TryGetFirst(input => input.Name == actionId, out var inputHandler))
            {
                key = inputHandler.Key;
                return true;
            }

            key = KeyCode.None;
            return false;
        }

        public static bool TryAddHandler(string actionId, KeyCode defaultKey, Action<ReferenceHub> listener)
        {
            if (TryGetListener(actionId, out _))
            {
                FLog.Warn($"Attempted to register an already existing handler ID: {actionId}");
                return false;
            }        

            if (TryGetListener(defaultKey, out _))
            {
                FLog.Warn($"Attempted to register an already existing handler key: {defaultKey}");
                return false;
            }

            m_Inputs.Add(new InputHandlerData(actionId, defaultKey, listener));
            FLog.Info($"Registered a new input handler: {actionId} ({defaultKey})");
            return true;
        }

        public static bool TryRemoveHandler(string actionId)
        {
            var removed = m_Inputs.RemoveWhere(data => data.Name == actionId);

            if (removed > 0)
            {
                FLog.Info($"Removed input handler: {actionId}");
                return true;
            }
            else
            {
                FLog.Warn($"Failed to remove input handler: {actionId}");
                return false;
            }
        }

        public static bool TryRemoveHandler(KeyCode key)
        {
            var removed = m_Inputs.RemoveWhere(data => data.Key == key);

            if (removed > 0)
            {
                FLog.Info($"Removed input handler: {key}");
                return true;
            }
            else
            {
                FLog.Warn($"Failed to remove input handler: {key}");
                return false;
            }
        }

        public static bool TryGetListener(string actionId, out Action<ReferenceHub> listener)
        {
            if (m_Inputs.TryGetFirst(data => data.Name == actionId, out var handler))
            {
                listener = handler.Listener;
                return true;
            }

            listener = null;
            return false;
        }

        public static bool TryGetListener(KeyCode key, out Action<ReferenceHub> listener)
        {
            if (m_Inputs.TryGetFirst(data => data.Key == key, out var handler))
            {
                listener = handler.Listener;
                return true;
            }

            listener = null;
            return false;
        }

        public static void KeyHandler(Player sender, KeyCode key)
        {
            if (m_PlayerBinds.Data.TryGetFirst(data => data.Key == key && data.OwnerId == sender.UserId, out var bind) 
                && TryGetListener(bind.ActionId, out var listener))
            {
                listener?.Invoke(sender.ReferenceHub);
            }
            else if (TryGetListener(key, out listener))
            {
                listener?.Invoke(sender.ReferenceHub);
            }
        }

        [Command("inputrecv", CommandType.PlayerConsole)]
        private static void InputRecv(Player sender, KeyCode key) => KeyHandler(sender, key);

        [Command("inputrepl", CommandType.PlayerConsole)]
        private static string InputRepl(Player sender, KeyCode key, string actionId)
        {
            if (TryReplaceKey(actionId, key, sender.UserId))
            {
                return $"Key bind replaced!";
            }
            else
            {
                return "Failed to replace key bind.";
            }
        }
    }
}