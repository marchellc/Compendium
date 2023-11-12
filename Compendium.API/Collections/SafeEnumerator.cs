using helpers;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Collections
{
    public class SafeEnumerator : IEnumerator
    {
        public readonly Array Target;

        public int Index = 0;
        public int PreviousIndex = 0;

        public event Action<int, int, object, object> OnAdvanced;

        public SafeEnumerator(IEnumerable target)
        {
            if (target is Array array)
                Target = array;
            else
            {
                var size = target.Count();

                Target = Array.CreateInstance(typeof(object), size);

                if (Target is null)
                    return;

                for (int i = 0; i < size; i++)
                    Target.SetValue(target.ElementOfIndex(i), i);
            }
        }

        public object Current
        {
            get
            {
                if (Target is null)
                    return null;

                if (Index < 0 || Index >= Target.Length)
                    return null;

                return Target.GetValue(Index);
            }
        }

        public object Previous
        {
            get
            {
                if (Target is null)
                    return null;

                if (PreviousIndex < 0 || PreviousIndex >= Target.Length)
                    return null;

                return Target.GetValue(PreviousIndex);
            }
        }

        public bool MoveNext()
        {
            if (Target is null)
                return false;

            PreviousIndex = Index;

            Index++;

            if (Index >= Target.Length)
                return false;

            OnAdvanced?.Invoke(PreviousIndex, Index, Current, Previous);

            return true;
        }

        public void Reset()
        {
            Index = 0;
            PreviousIndex = 0;
        }

        public static SafeEnumerator Get(IEnumerable target)
            => new SafeEnumerator(target);

        public static void Enumerate(IEnumerable target, bool includeNull, Action<object> action)
        {
            var enumerator = Get(target);

            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;

                if (value is null && !includeNull)
                    continue;

                try
                {
                    action?.Invoke(value);
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed while enumerating at index '{enumerator.Index}': {ex}");
                }
            }
        }
    }

    public class SafeEnumerator<T> : IEnumerator<T>
    {
        public T[] Target;

        public int Index = 0;
        public int PreviousIndex = 0;

        public object Lock = new object();

        public event Action<int, int, T, T> OnAdvanced;

        public SafeEnumerator(IEnumerable<T> target)
        {
            if (target is T[] array)
                Target = array;
            else
            {
                var size = target.Count();

                Target = new T[size];

                if (Target is null)
                    return;

                for (int i = 0; i < size; i++)
                    Target[i] = target.ElementAtOrDefault(i);
            }
        }

        public object Current
        {
            get
            {
                lock (Lock)
                {
                    if (Target is null)
                        return default;

                    if (Index < 0 || Index >= Target.Length)
                        return default;

                    return Target[Index];
                }
            }
        }

        T IEnumerator<T>.Current
        {
            get
            {
                lock (Lock)
                {
                    if (Target is null)
                        return default;

                    if (Index < 0 || Index >= Target.Length)
                        return default;

                    return Target[Index];
                }
            }
        }

        public T Previous
        {
            get
            {
                lock (Lock)
                {
                    if (Target is null)
                        return default;

                    if (PreviousIndex < 0 || PreviousIndex >= Target.Length)
                        return default;

                    return Target[PreviousIndex];
                }
            }
        }

        public bool MoveNext()
        {
            lock (Lock)
            {
                if (Target is null)
                    return false;

                PreviousIndex = Index;

                Index++;

                if (Index >= Target.Length)
                    return false;

                OnAdvanced?.Invoke(PreviousIndex, Index, (T)Current, Previous);

                return true;
            }
        }

        public void Reset()
        {
            lock (Lock)
            {
                Index = 0;
                PreviousIndex = 0;
            }
        }

        public void Dispose()
        {
            lock (Lock)
            {
                Target = null;
                Index = 0;
                PreviousIndex = 0;
            }

            Lock = null;
        }

        public static SafeEnumerator<T> Get(IEnumerable<T> target)
            => new SafeEnumerator<T>(target);

        public static void Enumerate(IEnumerable<T> target, bool includeNull, Action<T> action)
        {
            var enumerator = Get(target);

            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;

                if (value is null && !includeNull)
                    continue;

                try
                {
                    action?.Invoke((T)value);
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed while enumerating '{typeof(T).FullName}' at index '{enumerator.Index}': {ex}");
                }
            }
        }
    }
}
