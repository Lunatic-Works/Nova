using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Nova
{
    [Serializable]
    [ExportCustomType]
    public class Variables
    {
        private SortedDictionary<string, string> variables = new SortedDictionary<string, string>();

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

        public string Get(string name)
        {
            return variables.TryGetValue(name, out string varValue) ? varValue : null;
        }

        public void Set(string name, string value)
        {
            // Debug.Log(string.Format("Setting variable {0} to {1}", name, value));
            if (!variables.TryGetValue(name, out string varValue) || varValue != value)
            {
                variables[name] = value;
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

            return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(
                string.Join("\0", from pair in variables select pair.Key + "\0" + pair.Value)
            )));
        }

        public void CopyFrom(Variables variables)
        {
            this.variables = new SortedDictionary<string, string>(variables.variables);
            hash = variables.hash;
        }

        public void Reset()
        {
            variables.Clear();
            hash = "";
        }

        public override string ToString()
        {
            return string.Join(",", variables.Keys.Select(k => k + ":" + variables[k]));
        }
    }
}