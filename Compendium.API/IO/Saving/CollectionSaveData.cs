using System.Collections;
using System.Collections.Generic;

namespace Compendium.IO.Saving
{
    public class CollectionSaveData<TElement> : SimpleSaveData<List<TElement>>, IList<TElement>
    {
        public CollectionSaveData() { Value = new List<TElement>(); }

        public TElement this[int index] { get => Value[index]; set => Value[index] = value; }

        public int Count => Value.Count;

        public bool IsReadOnly => false;

        public void Add(TElement item)
            => Value.Add(item);

        public void Clear()
            => Value.Clear();

        public bool Contains(TElement item)
            => Value.Contains(item);

        public void CopyTo(TElement[] array, int arrayIndex)
            => Value.CopyTo(array, arrayIndex);

        public IEnumerator<TElement> GetEnumerator()
            => Value.GetEnumerator();

        public int IndexOf(TElement item)
            => Value.IndexOf(item);

        public void Insert(int index, TElement item)
            => Value.Insert(index, item);

        public bool Remove(TElement item)
            => Value.Remove(item);

        public void RemoveAt(int index)
            => Value.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator()
            => Value.GetEnumerator();
    }
}