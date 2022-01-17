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
        private SortedDictionary<string, VariableEntry> variables = new SortedDictionary<string, VariableEntry>();

        [NonSerialized] private ulong _hash;
        [NonSerialized] private bool needCalculateHash = true;

        public ulong hash
        {
            get
            {
                if (needCalculateHash)
                {
                    _hash = GetHash();
                    needCalculateHash = false;
                }

                return _hash;
            }
            private set
            {
                _hash = value;
                needCalculateHash = false;
            }
        }

        public VariableEntry Get(string name)
        {
            variables.TryGetValue(name, out var entry);
            return entry;
        }

        public void Set(string name, VariableType type, string value)
        {
            variables.TryGetValue(name, out var oldEntry);
            if (value == null)
            {
                if (oldEntry != null)
                {
                    variables.Remove(name);
                    needCalculateHash = true;
                }
            }
            else
            {
                if (oldEntry == null || oldEntry.type != type || oldEntry.value != value)
                {
                    variables[name] = new VariableEntry(type, value);
                    needCalculateHash = true;
                }
            }
        }

        private ulong GetHash()
        {
            unchecked
            {
                var x = 0UL;
                foreach (var pair in variables)
                {
                    foreach (var c in pair.Key)
                    {
                        x += c;
                        x *= 3074457345618258799UL;
                    }

                    x += (ulong)pair.Value.type;
                    x *= 3074457345618258799UL;

                    foreach (var c in pair.Value.value)
                    {
                        x += c;
                        x *= 3074457345618258799UL;
                    }
                }

                return x;
            }
        }

        public void CopyFrom(Variables variables)
        {
            this.variables = new SortedDictionary<string, VariableEntry>(variables.variables);
            needCalculateHash = true;
        }

        public void Reset()
        {
            variables.Clear();
            hash = 0UL;
        }

        public override string ToString()
        {
            return string.Join(",",
                from pair in variables select pair.Key + ":" + pair.Value.type + "=" + pair.Value.value);
        }
    }
}