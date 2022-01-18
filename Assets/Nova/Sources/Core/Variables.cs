using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova
{
    [ExportCustomType]
    public enum VariableType
    {
        Boolean,
        Number,
        String
    }

    [ExportCustomType]
    [Serializable]
    public class VariableEntry
    {
        public readonly VariableType type;
        public readonly string value;

        public VariableEntry(VariableType type, string value)
        {
            this.type = type;
            this.value = value;
        }
    }

    [ExportCustomType]
    [Serializable]
    public class Variables
    {
        private SortedDictionary<string, VariableEntry> dict = new SortedDictionary<string, VariableEntry>();

        [NonSerialized] private ulong _hash;
        [NonSerialized] private bool needCalculateHash = true;

        public ulong hash
        {
            get
            {
                if (needCalculateHash)
                {
                    _hash = GetHashULong();
                    needCalculateHash = false;
                }

                return _hash;
            }
        }

        // Knuth's golden ratio multiplicative hashing
        private ulong GetHashULong()
        {
            unchecked
            {
                var x = 0UL;
                foreach (var pair in dict)
                {
                    x += (ulong)pair.Key.GetHashCode();
                    x *= 11400714819323199563UL;
                    x += (ulong)pair.Value.type;
                    x *= 11400714819323199563UL;
                    x += (ulong)pair.Value.value.GetHashCode();
                    x *= 11400714819323199563UL;
                }

                return x;
            }
        }

        public VariableEntry Get(string name)
        {
            dict.TryGetValue(name, out var entry);
            return entry;
        }

        public void Set(string name, VariableType type, string value)
        {
            dict.TryGetValue(name, out var oldEntry);
            if (value == null)
            {
                if (oldEntry != null)
                {
                    dict.Remove(name);
                    needCalculateHash = true;
                }
            }
            else
            {
                if (oldEntry == null || oldEntry.type != type || oldEntry.value != value)
                {
                    dict[name] = new VariableEntry(type, value);
                    needCalculateHash = true;
                }
            }
        }

        public void CopyFrom(Variables variables)
        {
            dict = new SortedDictionary<string, VariableEntry>(variables.dict);
            needCalculateHash = true;
        }

        public void Clear()
        {
            dict.Clear();
            needCalculateHash = true;
        }

        public override string ToString()
        {
            return "Variables: " + string.Join(", ",
                from pair in dict select $"{pair.Key}:{pair.Value.type}={pair.Value.value}");
        }
    }
}