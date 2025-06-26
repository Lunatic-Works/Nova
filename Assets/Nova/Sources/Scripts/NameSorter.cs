using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nova
{
    public class NameSorter : MonoBehaviour
    {
        [SerializeField] private List<string> matchers;

        private class NaturalComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                int ix = 0, iy = 0;
                while (ix < x.Length && iy < y.Length)
                {
                    char cx = x[ix];
                    char cy = y[iy];

                    if (char.IsDigit(cx) && char.IsDigit(cy))
                    {
                        long nx = 0, ny = 0;
                        int sx = ix;
                        while (ix < x.Length && char.IsDigit(x[ix]))
                        {
                            ix++;
                        }

                        long.TryParse(x.Substring(sx, ix - sx), out nx);

                        int sy = iy;
                        while (iy < y.Length && char.IsDigit(y[iy]))
                        {
                            iy++;
                        }

                        long.TryParse(y.Substring(sy, iy - sy), out ny);

                        if (nx < ny) return -1;
                        if (nx > ny) return 1;
                    }
                    else
                    {
                        if (cx != cy)
                        {
                            return cx.CompareTo(cy);
                        }

                        ix++;
                        iy++;
                    }
                }

                if (x.Length > y.Length) return 1;
                if (x.Length < y.Length) return -1;
                return 0;
            }
        }

        private static IEnumerable<string> NaturalSort(IEnumerable<string> names)
        {
            return names.OrderBy(x => x, new NaturalComparer());
        }

        public IEnumerable<string> Sort(IEnumerable<string> names)
        {
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
