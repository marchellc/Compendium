using Compendium.Events;

using helpers.Attributes;
using helpers.Enums;
using helpers;

using PlayerStatsSystem;

using PluginAPI.Events;

using System;
using System.Collections.Generic;

namespace Compendium.EasyComponents
{
    public static class EasyComponentManager
    {
        private static readonly HashSet<EasyComponent> _components = new HashSet<EasyComponent>();
        private static readonly object _lock = new object();

        public static IReadOnlyCollection<EasyComponent> Components => _components;

        public static void Remove(EasyComponent component)
        {
            component.Stop();
            Lock(() => _components.RemoveWhere(c => c == component));
        }

        public static void Remove<TComponent>(ReferenceHub player) where TComponent : EasyComponent
        {
            if (TryGet<TComponent>(player, out var comp))
                Remove(comp);
        }

        public static EasyComponent Add(ReferenceHub player, EasyComponent component)
        {
            if (TryGet(player, component.GetType(), out component))
                return component;

            component.Start(player);

            Lock(() => _components.Add(component));

            return component;
        }

        public static TComponent Add<TComponent>(ReferenceHub player) where TComponent : EasyComponent, new()
        {
            if (TryGet(player, typeof(TComponent), out _))
                return default;

            var comp = new TComponent();

            comp.Start(player);

            Lock(() => _components.Add(comp));

            return comp;
        }

        public static bool TryAdd(ReferenceHub player, EasyComponent component)
        {
            if (TryGet(player, component.GetType(), out _))
                return false;

            component.Start(player);

            Lock(() => _components.Add(component));

            return true;
        }

        public static bool TryAdd<TComponent>(ReferenceHub player) where TComponent : EasyComponent, new()
        {
            if (TryGet(player, typeof(TComponent), out _))
                return false;

            var comp = new TComponent();

            comp.Start(player);

            Lock(() => _components.Add(comp));

            return true;
        }

        public static bool TryGet<TComponent>(ReferenceHub player, out TComponent component) where TComponent : EasyComponent
        {
            if (_components.TryGetFirst(c => c.Owner != null && c.Owner == player, out var comp) && comp is TComponent tCast)
            {
                component = tCast;
                return true;
            }

            component = default;
            return false;
        }

        public static bool TryGet(ReferenceHub player, Type type, out EasyComponent component)
            => _components.TryGetFirst(c => c.Owner != null && c.Owner == player && c.GetType() == type, out component);

        public static EasyComponent Get(ReferenceHub player, Type type)
            => TryGet(player, type, out var c) ? c : null;

        public static TComponent Get<TComponent>(ReferenceHub player) where TComponent : EasyComponent
            => TryGet<TComponent>(player, out var c) ? c : default;

        public static EasyComponent[] GetComponents(ReferenceHub player)
            => _components.Where<EasyComponent>(false, c => c.Owner != null && c.Owner == player).ToArray();

        [Reload]
        private static void Reload()
        {
            _components.ForEach(c =>
            {
                c.OnReloaded();
            });
        }

        [Unload]
        private static void Unload()
        {
            _components.ForEach(c =>
            {
                c.Stop();
            });

            Lock(() => _components.Clear());
        }

        private static void Lock(Action act)
        {
            lock (_lock)
                Calls.Delegate(act);
        }

        [Event]
        private static bool OnClassChanged(PlayerChangeRoleEvent ev)
        {
            var comps = GetComponents(ev.Player.ReferenceHub);
            var result = true;

            foreach (var comp in comps)
            {
                if (!comp.ExecuteChangeEvent(ev) && result)
                    result = false;
            }

            comps.ForEach(c =>
            {
                if (c.Flags.HasFlagFast(EasyComponentFlags.RemoveOnDeath))
                {
                    Remove(c);
                }
            });

            return result;
        }

        [Event]
        private static bool OnDamaged(PlayerDamageEvent ev)
        {
            ReferenceHub targetHub = null;

            if (ev.DamageHandler is AttackerDamageHandler attackerDamage)
                targetHub = attackerDamage.Attacker.Hub;

            if (targetHub is null && (targetHub = (ev.Player?.ReferenceHub ?? ev.Target?.ReferenceHub)) is null)
                return true;

            var comps = GetComponents(targetHub);
            var result = true;

            foreach (var comp in comps)
            {
                if (!comp.ExecuteDamageEvent(ev) && result)
                    result = false;
            }

            comps.ForEach(c =>
            {
                if (c.Flags.HasFlagFast(EasyComponentFlags.RemoveOnDamage))
                {
                    Remove(c);
                }
            });

            return result;
        }

        [Event]
        private static bool OnDeath(PlayerDeathEvent ev)
        {
            var ply = ev.Player ?? ev.Attacker;

            if (ply is null)
                return true;

            var comps = GetComponents(ply.ReferenceHub);
            var result = true;

            foreach (var comp in comps)
            {
                if (!comp.ExecuteDeathEvent(ev) && result)
                    result = false;
            }

            comps.ForEach(c =>
            {
                if (c.Flags.HasFlagFast(EasyComponentFlags.RemoveOnDeath))
                {
                    Remove(c);
                }
            });

            return result;
        }
    }
}