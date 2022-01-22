using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova
{
    [Serializable]
    public class IntervalDictionary<TKey, TValue>
    {
        private class TPair
        {
            public readonly TKey Key;
            public TValue Value;

            public TPair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public override bool Equals(object obj)
            {
                return obj is TPair other && Key.Equals(other.Key);
            }

            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }
        }

        private class KeyComparer : IComparer<TPair>
        {
            public TPair Lower { get; private set; }
            public TPair LowerOrEqual { get; private set; }
            public TPair Upper { get; private set; }

            private readonly IComparer<TKey> comparer;

            public KeyComparer(IComparer<TKey> comparer)
            {
                this.comparer = comparer;
            }

            public void Reset()
            {
                Lower = null;
                LowerOrEqual = null;
                Upper = null;
            }

            public int Compare(TPair x, TPair y)
            {
                var res = comparer.Compare(x.Key, y.Key);

                if (res > 0)
                {
                    Lower = y;
                }

                if (res >= 0)
                {
                    LowerOrEqual = y;
                }
                else
                {
                    Upper = y;
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
            set
            {
                comparer.Reset();
                var pair = new TPair(key, value);
                dict.Contains(pair);
                if (comparer.LowerOrEqual == null)
                {
                    dict.Add(pair);
                }
                else if (comparer.LowerOrEqual.Key.Equals(key))
                {
                    if (!comparer.LowerOrEqual.Value.Equals(value))
                    {
                        if (comparer.Lower != null && comparer.Lower.Value.Equals(value))
                        {
                            dict.Remove(comparer.LowerOrEqual);
                        }
                        else
                        {
                            comparer.LowerOrEqual.Value = value;
                        }

                        if (comparer.Upper != null && comparer.Upper.Value.Equals(value))
                        {
                            dict.Remove(comparer.Upper);
                        }
                    }
                }
                else
                {
                    if (!comparer.LowerOrEqual.Value.Equals(value))
                    {
                        dict.Add(pair);

                        if (comparer.Upper != null && comparer.Upper.Value.Equals(value))
                        {
                            dict.Remove(comparer.Upper);
                        }
                    }
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            comparer.Reset();
            var pair = new TPair(key, default);
            dict.Contains(pair);
            if (comparer.LowerOrEqual == null)
            {
                value = default;
                return false;
            }
            else
            {
                value = comparer.LowerOrEqual.Value;
                return true;
            }
        }

        public void Clear()
        {
            dict.Clear();
        }

        public override string ToString()
        {
            return "IntervalDictionary: " + string.Join(", ", from pair in dict select $"{pair.Key}:{pair.Value}");
        }
    }
}