using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public class VariableEntry : ISerializedData
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
    public class Variables : ISerializedData
    {
        private static string CheckName(string name)
        {
            if (name.StartsWith("v_", StringComparison.Ordinal))
            {
                return name.Substring(2);
            }
            else
            {
                Debug.LogWarning($"Nova: Variable name {name} should start with v_");
                return name;
            }
        }

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

        private ulong GetHashULong()
        {
            return Utils.HashList(dict.SelectMany(pair => new[] {pair.Key, pair.Value.type, pair.Value.value}));
        }

        public VariableEntry Get(string name)
        {
            name = CheckName(name);
            dict.TryGetValue(name, out var entry);
            return entry;
        }

        public T Get<T>(string name, T defaultValue = default)
        {
            var entry = Get(name);
            if (entry != null)
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
            name = CheckName(name);
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

        public void CloneFrom(Variables variables)
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
