using System.Collections;
using System.Collections.Generic;

namespace Compendium.Collections
{
    public class SafeAccessCollection<T> : IList<T>
    {
        public readonly object Lock;
        public readonly List<T> Sub;

        public T this[int index]
        {
            get
            {
                lock (Lock)
                {
                    if (index < 0 || index >= Sub.Count)
                        return default;

                    return Sub[index];
                }    
            }
            set
            {
                lock (Lock)
                {
                    Sub[index] = value;
                }
            }
        }

        public int Count => Sub.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            lock (Lock)
                Sub.Add(item);
        }

        public void Clear()
        {
            lock (Lock)
                Sub.Clear();    
        }

        public bool Contains(T item)
        {
            lock (Lock)
                return Sub.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (Lock)
                Sub.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator() => Sub.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Sub.GetEnumerator();

        public int IndexOf(T item)
        {
            lock (Lock)
                return Sub.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            lock (Lock)
                Sub.Insert(index, item);
        }

        public bool Remove(T item)
        {
            lock (Lock)
                return Sub.Remove(item);
        }

        public void RemoveAt(int index)
        {
            lock (Lock)
                Sub.RemoveAt(index);
        }
    }
}
