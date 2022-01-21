using System;
using System.Collections.Generic;

namespace Nova
{
    [Serializable]
    public class IntervalDictionary<TKey, TValue>
    {
        private struct TPair
        {
            public readonly TKey Key;
            public readonly TValue Value;

            public TPair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private class KeyComparer : IComparer<TPair>
        {
            public bool Found { get; private set; }
            public TPair LowerBound { get; private set; }

            private readonly IComparer<TKey> comparer;

            public KeyComparer(IComparer<TKey> comparer)
            {
                this.comparer = comparer;
            }

            public void Reset()
            {
                Found = false;
            }

            public int Compare(TPair x, TPair y)
            {
                var res = comparer.Compare(x.Key, y.Key);
                if (res >= 0)
                {
                    Found = true;
                    LowerBound = y;
                }

                return res;
            }
        }

        private readonly SortedSet<TPair> dict;
        [NonSerialized] private readonly KeyComparer comparer;

        public IntervalDictionary()
        {
            dict = new SortedSet<TPair>(new KeyComparer(Comparer<TKey>.Default));
            comparer = (KeyComparer)dict.Comparer;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                {
                    throw new KeyNotFoundException();
                }

                return value;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            comparer.Reset();
            var pair = new TPair(key, default);
            dict.Contains(pair);
            if (comparer.Found)
            {
                value = comparer.LowerBound.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public void Add(TKey key, TValue value)
        {
            comparer.Reset();
            var pair = new TPair(key, value);
            dict.Contains(pair);
            if (!comparer.Found || !comparer.LowerBound.Value.Equals(value))
            {
                dict.Add(pair);
            }
        }

        public void Clear()
        {
            dict.Clear();
        }
    }
}