using helpers.Dynamic;
using helpers.Extensions;

using MEC;

using System;
using System.Collections.Generic;

namespace Compendium
{
    public static class Calls
    {
        public static void NextFrame(Action action) => Timing.RunCoroutine(CallAfterFramesCoroutine(action, 1));
        public static void AfterFrames(int frames, Action action) => Timing.RunCoroutine(CallAfterFramesCoroutine(action, frames));

        public static void Delay(float delay, Action action) => Timing.CallDelayed(delay, action);

        public static void OnTrue(Action action, Func<bool> validator) => Timing.RunCoroutine(CallWhenTrueCoroutine(action, validator));
        public static void OnFalse(Action action, Func<bool> validator) => Timing.RunCoroutine(CallWhenFalseCoroutine(action, validator));

        public static void UntilFalse(Action action, Func<bool> validator, float? delay = null) => Timing.RunCoroutine(CallUntilFalseCoroutine(action, validator, delay));
        public static void UntilTrue(Action action, Func<bool> validator, float? delay = null) => Timing.RunCoroutine(CallUntilTrueCoroutine(action, validator, delay));

        public static void Repeat(Action action, int amount, float? delay = null) => Timing.RunCoroutine(CallTimesCoroutine(action, amount, delay));

        public static void IfTrue(Func<bool> validator, Action action)
        {
            if (validator())
                action?.Invoke();
        }

        public static void IfFalse(Func<bool> validator, Action action)
        {
            if (!validator())
                action?.Invoke();
        }

        private static IEnumerator<float> CallAfterFramesCoroutine(Action action, int frameCount)
        {
            for (var i = 0; i < frameCount; i++) 
                yield return Timing.WaitForOneFrame;

            action();
        }

        private static IEnumerator<float> CallWhenTrueCoroutine(Action action, Func<bool> validator)
        {
            yield return Timing.WaitUntilTrue(validator);
            action?.Invoke();
        }

        private static IEnumerator<float> CallWhenFalseCoroutine(Action action, Func<bool> validator)
        {
            yield return Timing.WaitUntilFalse(validator);
            action?.Invoke();
        }

        private static IEnumerator<float> CallUntilFalseCoroutine(Action action, Func<bool> validator, float? delay = null)
        {
            while (validator())
            {
                if (delay.HasValue) 
                    yield return Timing.WaitForSeconds(delay.Value);

                action?.Invoke();
            }
        }

        private static IEnumerator<float> CallUntilTrueCoroutine(Action action, Func<bool> validator, float? delay = null)
        {
            while (!validator())
            {
                if (delay.HasValue) 
                    yield return Timing.WaitForSeconds(delay.Value);

                action?.Invoke();
            }
        }

        private static IEnumerator<float> CallTimesCoroutine(Action action, int amount, float? delay = null)
        {
            for (int i = 0; i < amount; i++)
            {
                if (delay.HasValue) 
                    yield return Timing.WaitForSeconds(delay.Value);

                action?.Invoke();
            }
        }

        public static void Delegate(Delegate del, params object[] args)
        {
            try
            {
                del.Method.InvokeDynamic(del.Target, args);
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to execute delegate: {del.Method.ToLogName()}");
                Plugin.Error(ex);
            }
        }

        public static TResult Delegate<TResult>(Delegate del, params object[] args)
        {
            try
            {
                return del.Method.InvokeDynamicResult<TResult>(del.Target, args);
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to execute delegate: {del.Method.ToLogName()}");
                Plugin.Error(ex);
            }

            return default;
        }

        public static void Call(Func<bool> validator, Action ifFalse, Action ifTrue)
        {
            if (!validator())
                Delegate(ifFalse);
            else
                Delegate(ifTrue);
        }
    }
}