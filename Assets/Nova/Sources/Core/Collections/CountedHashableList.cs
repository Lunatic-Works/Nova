using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nova
{
    /// <summary>
    /// A hashable list that supports getting the occurrence count of an element at a specified index with O(1) time.
    /// </summary>
    public class CountedHashableList<T> : IEnumerable<KeyValuePair<T, int>>
    {
        protected readonly List<KeyValuePair<T, int>> list = new List<KeyValuePair<T, int>>();
        private readonly Dictionary<T, int> valueCounts = new Dictionary<T, int>();

        protected ulong _hash;
        protected bool needCalculateHash = true;

        public ulong Hash
        {
            get
            {
                if (needCalculateHash)
                {
                    _hash = GetHashULong(0, Count);
                    needCalculateHash = false;
                }

                return _hash;
            }
        }

        // Knuth's golden ratio multiplicative hashing
        public virtual ulong GetHashULong(int index, int count)
        {
            unchecked
            {
                var x = 0UL;
                for (var i = index; i < index + count; ++i)
                {
                    x += (ulong)list[i].Key.GetHashCode();
                    x *= 11400714819323199563UL;
                }

                return x;
            }
        }

        private void UpdateHashULong(T item)
        {
            unchecked
            {
                _hash += (ulong)item.GetHashCode();
                _hash *= 11400714819323199563UL;
            }
        }

        public int Count => list.Count;

        public KeyValuePair<T, int> this[int index] => list[index];

        private void AddCount(T item)
        {
            if (valueCounts.ContainsKey(item))
            {
                ++valueCounts[item];
            }
            else
            {
                valueCounts[item] = 1;
            }
        }

        public void Add(T item)
        {
            AddCount(item);
            list.Add(new KeyValuePair<T, int>(item, valueCounts[item]));
            if (!needCalculateHash)
            {
                UpdateHashULong(item);
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public virtual void RemoveRange(int index, int count)
        {
            for (var i = index; i < index + count; ++i)
            {
                --valueCounts[list[i].Key];
            }

            list.RemoveRange(index, count);
            needCalculateHash = true;
        }

        public virtual void Clear()
        {
            list.Clear();
            valueCounts.Clear();
            needCalculateHash = true;
        }

        public int FindLastIndex(Predicate<KeyValuePair<T, int>> match)
        {
            return list.FindLastIndex(match);
        }

        public KeyValuePair<T, int> Last()
        {
            return list[Count - 1];
        }

        public KeyValuePair<T, int> GetCounted(T item)
        {
            return new KeyValuePair<T, int>(item, valueCounts[item]);
        }

        public IEnumerator<KeyValuePair<T, int>> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "CountedHashableList: " + string.Join(", ", from pair in list select $"{pair.Key}:{pair.Value}");
        }
    }
}
