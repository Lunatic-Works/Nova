using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

    [Serializable]
    [ExportCustomType]
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

    [Serializable]
    [ExportCustomType]
    public class Variables
    {
        private SortedDictionary<string, VariableEntry> variables = new SortedDictionary<string, VariableEntry>();

        private string _hash = "";
        private bool needCalculateHash;

        public string hash
        {
            get
            {
                if (needCalculateHash) _hash = CalculateHash();
                return _hash;
            }
            private set
            {
                _hash = value;
                needCalculateHash = false;
            }
        }

        [NonSerialized] private HashAlgorithm algorithm = SHA256.Create();

        public VariableEntry Get(string name)
        {
            variables.TryGetValue(name, out var entry);
            return entry;
        }

        public void Set(string name, VariableType type, string value)
        {
            variables.TryGetValue(name, out var oldEntry);
            if (oldEntry == null || oldEntry.type != type || oldEntry.value != value)
            {
                variables[name] = new VariableEntry(type, value);
                needCalculateHash = true;
            }
        }

        private string CalculateHash()
        {
            needCalculateHash = false;

            if (variables.Count == 0)
            {
                return "";
            }

            string hash = Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(
                string.Join("\0", from pair in variables select pair.Key + "\0" + pair.Value.type + "\0" + pair.Value.value)
            )));

            // Debug.Log("CalculateHash");
            // foreach (var pair in variables)
            // {
            //     Debug.LogFormat("{0} {1} {2}", pair.Key, pair.Value.type, pair.Value.value);
            // }
            // Debug.Log(hash);

            return hash;
        }

        public void CopyFrom(Variables variables)
        {
            this.variables = new SortedDictionary<string, VariableEntry>(variables.variables);
            hash = variables.hash;
        }

        public void Reset()
        {
            variables.Clear();
            hash = "";
        }

        public override string ToString()
        {
            return string.Join(",", from pair in variables select pair.Key + ":" + pair.Value.type + ":" + pair.Value.value);
        }
    }
}