using BetterCommands;
using BetterCommands.Management;

using Compendium.Helpers.Events;
using Compendium.State;
using Compendium.State.Base;

using helpers.Extensions;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Compendium.Input
{
    public class InputManager : RequiredStateBase
    {
        private static readonly Dictionary<KeyCode, HashSet<Tuple<byte,Action<KeyCode, ReferenceHub, InputManager>>>> m_GlobalHandlers = new Dictionary<KeyCode, HashSet<Tuple<byte, Action<KeyCode, ReferenceHub, InputManager>>>>();
        private readonly Dictionary<KeyCode, HashSet<Tuple<byte, bool, Action<KeyCode, ReferenceHub, InputManager>>>> m_KeyHandlers = new Dictionary<KeyCode, HashSet<Tuple<byte, bool, Action<KeyCode, ReferenceHub, InputManager>>>>();
        private KeyCode m_LastKey;     

        public override StateFlags Flags => StateFlags.DisableUpdate;
        public override string Name => "Input";

        public KeyCode LastKey => m_LastKey;

        static InputManager()
        {
            ServerEventType.RoundEnd.GetProvider()?.Add(OnRoundEnd);
        }

        public static void AddGlobalHandler(KeyCode keyCode, Action<KeyCode, ReferenceHub, InputManager> handler, byte priority = byte.MinValue)
        {
            if (!m_GlobalHandlers.ContainsKey(keyCode))
                m_GlobalHandlers.Add(keyCode, new HashSet<Tuple<byte, Action<KeyCode, ReferenceHub, InputManager>>>());

            m_GlobalHandlers[keyCode].Add(new Tuple<byte, Action<KeyCode, ReferenceHub, InputManager>>(priority, handler));
            m_GlobalHandlers[keyCode] = m_GlobalHandlers[keyCode].OrderByDescending(x => x.Item1).ToHashSet();
        }

        public override void OnUnloaded() => ClearHandlers();

        public void AddKeyHandler(KeyCode keyCode, Action<KeyCode, ReferenceHub, InputManager> handler, byte priority = byte.MinValue)
        {
            if (!m_KeyHandlers.ContainsKey(keyCode))
                m_KeyHandlers.Add(keyCode, new HashSet<Tuple<byte, bool, Action<KeyCode, ReferenceHub, InputManager>>>());

            m_KeyHandlers[keyCode].Add(new Tuple<byte, bool, Action<KeyCode, ReferenceHub, InputManager>>(priority, false, handler));
            m_KeyHandlers[keyCode] = m_KeyHandlers[keyCode].OrderByDescending(x => x.Item1).ToHashSet();
        }

        public void AddTemporaryKeyHandler(KeyCode keyCode, Action<KeyCode, ReferenceHub, InputManager> handler, byte priority = byte.MinValue)
        {
            if (!m_KeyHandlers.ContainsKey(keyCode))
                m_KeyHandlers.Add(keyCode, new HashSet<Tuple<byte, bool, Action<KeyCode, ReferenceHub, InputManager>>>());

            m_KeyHandlers[keyCode].Add(new Tuple<byte, bool, Action<KeyCode, ReferenceHub, InputManager>>(priority, true, handler));
            m_KeyHandlers[keyCode] = m_KeyHandlers[keyCode].OrderByDescending(x => x.Item1).ToHashSet();
        }

        [Command("sendkey", BetterCommands.Management.CommandType.PlayerConsole)]
        public static string OnCommand(ReferenceHub sender, KeyCode keyCode)
        {
            if (sender.TryGetState<InputManager>(out var input))
            {
                input.m_LastKey = keyCode;

                if (m_GlobalHandlers.TryGetValue(keyCode, out var globalHandlers))
                {
                    globalHandlers.ForEach(handler =>
                    {
                        handler.Item2?.Invoke(keyCode, input.Player, input);
                    });
                }

                if (input.m_KeyHandlers.TryGetValue(keyCode, out var handlers))
                {
                    handlers.ForEach(handler =>
                    {
                        handler.Item3?.Invoke(keyCode, input.Player, input);
                    });

                    handlers.RemoveWhere(handler => handler.Item2);
                }

                return $"Key recorded: {keyCode}";
            }
            else
            {
                return $"Missing input manager!";
            }
        }

        private void ClearHandlers() => m_KeyHandlers.Clear();

        private static void OnRoundEnd()
        {
            m_GlobalHandlers.Clear();
        }
    }
}