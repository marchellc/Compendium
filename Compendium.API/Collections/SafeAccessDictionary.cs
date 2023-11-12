using helpers;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Collections
{
    public class SafeAccessDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public readonly object Lock;
        public readonly Dictionary<TKey, TValue> Sub;

        public TValue this[TKey key]
        {
            get
            {
                lock (Lock)
                {
                    if (Sub.ContainsKey(key))
                        return Sub[key];

                    return default;
                }
            }
            set
            {
                lock (Lock)
                {
                    Sub[key] = value;
                }
            }
        }

        public ICollection<TKey> Keys => Sub.Keys;
        public ICollection<TValue> Values => Sub.Values;

        public int Count => Sub.Count;
        public bool IsReadOnly => false;

        public SafeAccessDictionary()
        {
            Lock = new object();
            Sub = new Dictionary<TKey, TValue>();
        }

        public SafeAccessDictionary(IEnumerable<KeyValuePair<TKey, TValue>> list)
        {
            Lock = new object();
            Sub = new Dictionary<TKey, TValue>(list.ToDictionary());
        }

        public SafeAccessDictionary(IDictionary<TKey, TValue> dictionary)
        {
            Lock = new object();
            Sub = new Dictionary<TKey, TValue>(dictionary);
        }

        public SafeAccessDictionary(int size)
        {
            Lock = new object();
            Sub = new Dictionary<TKey, TValue>(size);
        }

        public void Add(TKey key, TValue value)
        {
            lock (Lock)
                Sub[key] = value;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            lock (Lock)
                Sub[item.Key] = item.Value;
        }

        public void Clear()
        {
            lock (Lock)
                Sub.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (Lock)
                return Sub.ContainsKey(item.Key) && Sub.ContainsValue(item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            lock (Lock)
                return Sub.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (Lock)
            {
                for (int i = arrayIndex; i < array.Length; i++)
                    array[i] = Sub.ElementAtOrDefault(i);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Sub.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Sub.GetEnumerator();

        public bool Remove(TKey key)
        {
            lock (Lock)
                return Sub.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (Lock)
                return Sub.Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (Lock)
                return Sub.TryGetValue(key, out value);
        }
    }
}
