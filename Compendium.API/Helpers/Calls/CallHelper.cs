using MEC;

using System;
using System.Collections.Generic;

namespace Compendium.Helpers.Calls
{
    public static class CallHelper
    {
        public static void CallNextFrame(Action action) => Timing.RunCoroutine(CallAfterFramesCoroutine(action, 1));
        public static void CallAfterFrames(Action action, int frameCount) => Timing.RunCoroutine(CallAfterFramesCoroutine(action, frameCount));

        public static void CallWithDelay(Action action, float delay) => Timing.CallDelayed(delay, action);

        public static void CallWhenTrue(Action action, Func<bool> validator) => Timing.RunCoroutine(CallWhenTrueCoroutine(action, validator));
        public static void CallWhenFalse(Action action, Func<bool> validator) => Timing.RunCoroutine(CallWhenFalseCoroutine(action, validator));

        public static void CallUntilFalse(Action action, Func<bool> validator, float? delay = null) => Timing.RunCoroutine(CallUntilFalseCoroutine(action, validator, delay));
        public static void CallUntilTrue(Action action, Func<bool> validator, float? delay = null) => Timing.RunCoroutine(CallUntilTrueCoroutine(action, validator, delay));

        public static void CallTimes(Action action, int amount, float? delay = null) => Timing.RunCoroutine(CallTimesCoroutine(action, amount, delay));

        public static void CallIfTrue(Func<bool> validator, Action action)
        {
            if (validator())
                action?.Invoke();
        }

        public static void CallIfFalse(Func<bool> validator, Action action)
        {
            if (!validator())
                action?.Invoke();
        }

        private static IEnumerator<float> CallAfterFramesCoroutine(Action action, int frameCount)
        {
            for (var i = 0; i < frameCount; i++) yield return Timing.WaitForOneFrame;
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
                if (delay.HasValue) yield return Timing.WaitForSeconds(delay.Value);
                action?.Invoke();
            }
        }

        private static IEnumerator<float> CallUntilTrueCoroutine(Action action, Func<bool> validator, float? delay = null)
        {
            while (!validator())
            {
                if (delay.HasValue) yield return Timing.WaitForSeconds(delay.Value);
                action?.Invoke();
            }
        }

        private static IEnumerator<float> CallTimesCoroutine(Action action, int amount, float? delay = null)
        {
            for (int i = 0; i < amount; i++)
            {
                if (delay.HasValue) yield return Timing.WaitForSeconds(delay.Value);
                action?.Invoke();
            }
        }

        public static TResult SafeDelegate<TResult, TInput1, TInput2>(Func<TInput1, TInput2, TResult> del, TInput1 input1, TInput2 input2, TResult defResult = default)
        {
            try
            {
                if (del is null)
                    return defResult;

                return del(input1, input2);
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to execute Func<{typeof(TInput1).FullName}, {typeof(TInput2).FullName}, {typeof(TResult).FullName}> !");
                Plugin.Error(ex);

                return defResult;
            }
        }
    }
}