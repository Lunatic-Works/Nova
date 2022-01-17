using System;
using System.Collections.Generic;

namespace Nova
{
    public class CountedHashableList<T> : HashableList<KeyValuePair<T, int>>
    {
        private readonly Dictionary<T, int> valueCount = new Dictionary<T, int>();

        public new KeyValuePair<T, int> this[int index]
        {
            get => base[index];
            set => throw new NotImplementedException();
        }

        private void AddCount(T item)
        {
            if (valueCount.ContainsKey(item))
            {
                ++valueCount[item];
            }
            else
            {
                valueCount[item] = 1;
            }
        }

        public void Add(T item)
        {
            AddCount(item);
            base.Add(new KeyValuePair<T, int>(item, valueCount[item]));
        }

        public new void Add(KeyValuePair<T, int> item)
        {
            throw new NotImplementedException();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public new void AddRange(IEnumerable<KeyValuePair<T, int>> collection)
        {
            throw new NotImplementedException();
        }

        public new void RemoveRange(int index, int count)
        {
            for (var i = index; i < index + count; ++i)
            {
                --valueCount[base[i].Key];
            }

            base.RemoveRange(index, count);
        }

        public new void Clear()
        {
            base.Clear();
            valueCount.Clear();
        }

        public KeyValuePair<T, int> GetCounted(T item)
        {
            return new KeyValuePair<T, int>(item, valueCount[item]);
        }
    }
}