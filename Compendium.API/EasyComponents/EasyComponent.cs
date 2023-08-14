using helpers.Enums;
using helpers.Time;

using MEC;
using PluginAPI.Events;
using System;
using System.Collections.Generic;

namespace Compendium.EasyComponents
{
    public class EasyComponent
    {
        private ReferenceHub _owner;

        private CoroutineHandle _ticker;
        private DateTime? _lastTick;

        private float _tickDuration;

        public ReferenceHub Owner => _owner;

        public CoroutineHandle TickHandle => _ticker;
        public DateTime LastTickTime => _lastTick.HasValue ? _lastTick.Value : DateTime.MinValue;

        public bool IsTicking => Timing.IsRunning(_ticker);

        public float TickDuration => _tickDuration;

        public virtual EasyComponentFlags Flags { get; }

        public virtual float TickRate { get; } = 0.01f;

        public virtual void OnStarted() { }
        public virtual void OnStopped() { }
        public virtual void OnReloaded() { }
        public virtual void OnTicked(float time) { }

        public virtual bool OnDamaged(PlayerDamageEvent ev) { return true; }
        public virtual bool OnDied(PlayerDeathEvent ev) { return true; }
        public virtual bool OnClassChanged(PlayerChangeRoleEvent ev) { return true; }

        internal void Start(ReferenceHub owner)
        {
            _owner = owner;

            OnStarted();

            if (IsTicking)
                return;

            _ticker = Timing.RunCoroutine(Ticker());
        }

        internal void Stop()
        {
            Timing.KillCoroutines(_ticker);

            OnStopped();

            _ticker = default;
            _owner = null;
            _lastTick = null;
        }

        internal bool ExecuteDeathEvent(PlayerDeathEvent ev)
        {
            try
            {
                return OnDied(ev);
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to execute damage event in component ({GetType().FullName}) of player ({Owner?.GetLogName(true, false)})\n{ex}");
                return true;
            }
        }

        internal bool ExecuteDamageEvent(PlayerDamageEvent ev)
        {
            try
            {
                return OnDamaged(ev);
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to execute death event in component ({GetType().FullName}) of player ({Owner?.GetLogName(true, false)})\n{ex}");
                return true;
            }
        }

        internal bool ExecuteChangeEvent(PlayerChangeRoleEvent ev)
        {
            try
            {
                return OnClassChanged(ev);
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to execute class change event in component ({GetType().FullName}) of player ({Owner?.GetLogName(true, false)})\n{ex}");
                return true;
            }
        }

        internal IEnumerator<float> Ticker()
        {
            while (true)
            {
                if (!Flags.HasFlagFast(EasyComponentFlags.DisableTicks))
                {
                    var time = TickRate;

                    if (time > 0f)
                        yield return Timing.WaitForSeconds(time);

                    if (!_lastTick.HasValue)
                        _lastTick = TimeUtils.LocalTime;

                    _tickDuration = (TimeUtils.LocalTime - _lastTick.Value).Milliseconds;

                    try
                    {
                        OnTicked(time);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Error($"Failed to execute tick in component ({GetType().FullName}) of player ({Owner?.GetLogName(true, false) ?? "null owner"})\n{ex}");
                    }
                }
            }
        }
    }
}