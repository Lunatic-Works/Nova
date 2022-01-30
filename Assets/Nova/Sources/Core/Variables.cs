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
        public readonly object value;

        public VariableEntry(VariableType type, object value)
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

        public T Get<T>(string name, T defaultValue = default)
        {
            if (dict.TryGetValue(name, out var entry))
            {
                return (T)Convert.ChangeType(entry.value, typeof(T));
            }
            else
            {
                return defaultValue;
            }
        }

        public void Set(string name, VariableType type, object value)
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
                if (oldEntry == null || oldEntry.type != type || !oldEntry.value.Equals(value))
                {
                    dict[name] = new VariableEntry(type, value);
                    needCalculateHash = true;
                }
            }
        }

        public void Set<T>(string name, T value)
        {
            var t = typeof(T);
            if (value == null)
            {
                Set(name, VariableType.String, null);
            }
            else if (t == typeof(bool))
            {
                Set(name, VariableType.Boolean, value);
            }
            else if (Utils.IsNumericType(t))
            {
                Set(name, VariableType.Number, value);
            }
            else if (t == typeof(string))
            {
                Set(name, VariableType.String, value);
            }
            else
            {
                throw new ArgumentException(
                    $"Nova: Variable can only be bool, numeric types, string, or null, but found {t}: {value}");
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