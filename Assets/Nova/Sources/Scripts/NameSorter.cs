using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nova
{
    public class NameSorter : MonoBehaviour
    {
        [SerializeField] List<string> matchers;
        [SerializeField] private int padWidth = 3;

        private IEnumerable<string> NaturalSort(IEnumerable<string> names)
        {
            return names.OrderBy(x => Regex.Replace(x, @"\d+", m => m.Value.PadLeft(padWidth, '0')));
        }

        public IEnumerable<string> Sort(IEnumerable<string> names)
        {
            if (matchers.Count <= 0)
            {
                return names;
            }

            var buckets = new Dictionary<string, List<string>>();
            foreach (var matcher in matchers)
            {
                buckets[matcher] = new List<string>();
            }

            buckets["__default"] = new List<string>();

            foreach (var name in names)
            {
                bool matched = false;
                foreach (var matcher in matchers)
                {
                    if (Regex.IsMatch(name, matcher))
                    {
                        buckets[matcher].Add(name);
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    buckets["__default"].Add(name);
                }
            }

            return buckets.Values.SelectMany(NaturalSort);
        }
    }
}
