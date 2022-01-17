using System;
using System.Collections.Generic;

namespace Nova
{
    public class HashableList<T>
    {
        private readonly List<T> list = new List<T>();

        private ulong _hash;
        private bool needCalculateHash = true;

        public ulong Hash
        {
            get
            {
                if (needCalculateHash)
                {
                    _hash = GetHash(0, Count);
                    needCalculateHash = false;
                }

                return _hash;
            }
        }

        public ulong GetHash(int index, int count)
        {
            unchecked
            {
                var x = 0UL;
                for (var i = index; i < index + count; ++i)
                {
                    x += (ulong)list[i].GetHashCode();
                    x *= 3074457345618258799UL;
                }

                return x;
            }
        }

        public int Count => list.Count;

        public T this[int index]
        {
            get => list[index];
            set
            {
                list[index] = value;
                needCalculateHash = true;
            }
        }

        public void Add(T item)
        {
            list.Add(item);
            needCalculateHash = true;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            list.AddRange(collection);
            needCalculateHash = true;
        }

        public void RemoveRange(int index, int count)
        {
            list.RemoveRange(index, count);
            needCalculateHash = true;
        }

        public void Clear()
        {
            list.Clear();
            needCalculateHash = true;
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return list.FindLastIndex(match);
        }

        public List<T> CopyToList()
        {
            return new List<T>(list);
        }

        public T Last()
        {
            return list[Count - 1];
        }

        public override string ToString()
        {
            return "HashableList: " + string.Join(", ", list);
        }
    }
}