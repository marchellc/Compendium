using BetterCommands;

using Compendium.Attributes;

using helpers.Extensions;
using helpers.IO.Binary;

using PluginAPI.Core;

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace Compendium.Common.Input
{
    public static class InputHandler
    {
        private static readonly HashSet<InputHandlerData> m_Data = new HashSet<InputHandlerData>();
        private static readonly HashSet<InputHandlerPlayerData> m_PlayerData = new HashSet<InputHandlerPlayerData>();

        public static string Path => $"{Plugin.Handler.PluginDirectoryPath}/inputs";

        public static void Save()
        {
            var binary = new BinaryImage();

            binary.TryStore(m_PlayerData);
            binary.Save(Path);
        }

        [InitOnLoad]
        public static void Load()
        {
            if (!File.Exists(Path))
            {
                Save();
                return;
            }

            var binary = new BinaryImage();

            binary.Load(Path);

            m_PlayerData.Clear();

            if (!binary.TryRetrieve<HashSet<InputHandlerPlayerData>>(out var data))
                return;

            m_PlayerData.AddRange(data);
        }

        public static bool TryOverride(string targetId, string targetName, KeyCode newKey)
        {
            if (m_PlayerData.TryGetFirst(data => data.Name == targetName && data.TargetId == targetId, out var playerData))
            {
                playerData.Key = newKey;
                return true;
            }
            else
            {
                m_PlayerData.Add(new InputHandlerPlayerData(targetId, targetName, newKey));
                return true;
            }
        }

        public static bool TryAdd(string name, KeyCode key, Action<ReferenceHub, KeyCode> handler)
        {
            if (TryGetHandler(name, out _))
            {
                Log.Warning($"Attempted to register a duplicate handler: {name}", "Input Handler");
                return false;
            }

            if (TryGetHandler(key, out _))
            {
                Log.Warning($"Attempted to register a duplicate handler for key: {key}", "Input Handler");
                return false;
            }

            return m_Data.Add(new InputHandlerData(name, key, handler));
        }

        public static bool TryRemove(string name)
        {
            if (m_Data.RemoveWhere(data => data.Name == name) > 0)
            {
                return true;
            }
            else
            {
                Log.Warning($"Failed to remove handler: {name}", "Input Handler");
                return false;
            }
        }

        public static bool TryRemove(KeyCode key)
        {
            if (m_Data.RemoveWhere(data => data.DefaultKey == key) > 0)
            {
                return true;
            }
            else
            {
                Log.Warning($"Failed to remove handler for key: {key}", "Input Handler");
                return false;
            }
        }

        public static bool TryInvoke(ReferenceHub hub, KeyCode key)
        {
            if (TryGetPlayerHandler(hub, key, out var playerData))
            {
                if (TryGetHandler(playerData.Name, out var data))
                {
                    data.Receive(hub, key);
                    return true;
                }
                else
                {
                    Log.Warning($"Failed to retrieve handler from player data: {playerData.Name}", "Input Handler");
                }
            }

            if (TryGetHandler(key, out var handler))
            {
                handler.Receive(hub, key);
                return true;
            }

            return false;
        }

        public static bool TryGetHandler(string name, out InputHandlerData data)
        {
            if (m_Data.TryGetFirst(x => x.Name == name, out data))
            {
                return true;
            }

            data = default;
            return false;
        }

        public static bool TryGetHandler(KeyCode key, out InputHandlerData data)
        {
            if (m_Data.TryGetFirst(x => x.DefaultKey == key, out data))
            {
                return true;
            }

            data = default;
            return false;
        }

        public static bool TryGetPlayerHandler(ReferenceHub hub, KeyCode key, out InputHandlerPlayerData playerData)
        {
            if (m_PlayerData.TryGetFirst(data => data.TargetId == hub.characterClassManager.UserId && data.Key == key, out playerData))
            {
                return true;
            }

            playerData = default;
            return false;
        }

        public static bool TryGetPlayerData(ReferenceHub hub, string name, out InputHandlerPlayerData playerData)
        {
            if (m_PlayerData.TryGetFirst(data => data.TargetId == hub.characterClassManager.UserId && data.Name == name, out playerData))
            {
                return true;
            }

            playerData = default;
            return false;
        }

        [Command("inputrecv", CommandType.PlayerConsole)]
        private static void InputCommand(ReferenceHub sender, KeyCode key) => TryInvoke(sender, key);

        [Command("inputset", CommandType.PlayerConsole)]
        private static string SetCommand(Player sender, string action, KeyCode key)
        {
            if (TryOverride(sender.UserId, action, key))
            {
                return $"Succesfully mapped action {action} to {key}!";
            }
            else
            {
                return $"Failed to map action {action} to key {key}!";
            }
        }
    }
}