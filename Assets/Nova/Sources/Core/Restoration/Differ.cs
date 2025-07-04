using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    // An O(ND) diff algorithm: http://www.xmailserver.org/diff2.pdf
    public class Differ
    {
        private static readonly Exception Bug = new Exception("Nova: Differ internal bug.");

        private readonly ulong[] oldHashes, newHashes;

        // pseudo array of V[d][k]
        // the X value of furthest reached point for a d-path on diag-k
        private readonly List<int> v = new List<int>();
        private readonly List<int> _inserts = new List<int>();
        private readonly List<int> _deletes = new List<int>();
        private readonly List<int> _remap = new List<int>();
        private readonly List<int> _leftMap = new List<int>();
        private readonly List<int> _rightMap = new List<int>();

        // remap[i] == j means old list index j maps to new list index i
        // new item in new list remaps to index -1
        public IReadOnlyList<int> remap => _remap;

        // leftMap[i] is new index "left" of old index i
        public IReadOnlyList<int> leftMap => _leftMap;

        // rightMap[i] is new index "right" of old index i
        public IReadOnlyList<int> rightMap => _rightMap;

        public int distance { get; private set; }

        // return V[d][k]
        private int V(int d, int k)
        {
            if (k > d || k < -d || ((k + d) % 2 != 0))
            {
                throw Bug;
            }

            return v[(d + 1) * d / 2 + (k + d) / 2];
        }

        public Differ(ulong[] oldHashes, ulong[] newHashes)
        {
            this.oldHashes = oldHashes;
            this.newHashes = newHashes;
            distance = -1;
        }

        // find the shortest path and calculate V array
        // return the path length and the end point
        private int CalcV(out int x, out int y)
        {
            var xMax = oldHashes.Length + newHashes.Length;
            var xMid = oldHashes.Length;
            var yMax = newHashes.Length;
            // calculate V[d][k] with greedy algorithm
            for (int d = 0; d <= xMax + yMax; d++)
            {
                for (int k = -d; k <= d; k += 2)
                {
                    x = 0;
                    if (k > -d)
                    {
                        x = Math.Max(x, V(d - 1, k - 1) + 1);
                    }

                    if (k < d)
                    {
                        x = Math.Max(x, V(d - 1, k + 1));
                    }

                    y = x - k;
                    while (x < xMax && y < yMax && (x >= xMid || oldHashes[x] == newHashes[y]))
                    {
                        x++;
                        y++;
                    }

                    v.Add(x);
                    if (x >= xMid && y >= yMax)
                    {
                        return d;
                    }
                }
            }

            throw Bug;
        }

        private void CalcInsertsDeletes(int k)
        {
            for (int d = distance - 1; d >= 0; d--)
            {
                if (k + 1 > d || (k - 1 >= -d && V(d, k + 1) < V(d, k - 1) + 1))
                {
                    k--;
                    var x = V(d, k);
                    _deletes.Add(x);
                }
                else
                {
                    k++;
                    var x = V(d, k);
                    _inserts.Add(x - k);
                }
            }

            _deletes.Reverse();
            _inserts.Reverse();
        }

        private void CalcRemap()
        {
            int x = 0, i = 0, j = 0;
            for (var y = 0; y < newHashes.Length; y++)
            {
                while (i < _deletes.Count && _deletes[i] == x)
                {
                    i++;
                    x++;
                }

                if (j < _inserts.Count && _inserts[j] == y)
                {
                    _remap.Add(-1);
                    j++;
                }
                else
                {
                    _remap.Add(x < oldHashes.Length ? x : -1);
                    x++;
                }
            }
        }

        private void CalcLeftMap()
        {
            var x = oldHashes.Length - 1;
            for (var y = newHashes.Length - 1; y >= 0; y--)
            {
                if (_remap[y] == -1)
                {
                    continue;
                }

                for (; x >= _remap[y]; x--)
                {
                    _leftMap.Add(y);
                }
            }

            for (; x >= 0; x--)
            {
                _leftMap.Add(-1);
            }

            _leftMap.Reverse();
        }

        private void CalcRightMap()
        {
            var x = 0;
            for (var y = 0; y < newHashes.Length; y++)
            {
                if (_remap[y] == -1)
                {
                    continue;
                }

                for (; x < oldHashes.Length && x <= _remap[y]; x++)
                {
                    _rightMap.Add(y);
                }
            }

            for (; x < oldHashes.Length; x++)
            {
                _rightMap.Add(newHashes.Length);
            }
        }

        private void CalcNaiveRemap()
        {
            Debug.Log("Nova: Fallback to naive remap.");

            _remap.Clear();
            _leftMap.Clear();
            _rightMap.Clear();

            var minLength = Math.Min(oldHashes.Length, newHashes.Length);
            for (var i = 0; i < minLength; ++i)
            {
                _remap.Add(i);
                _leftMap.Add(i);
                _rightMap.Add(i);
            }

            for (var i = 0; i < oldHashes.Length - minLength; ++i)
            {
                _remap.Add(-1);
            }

            for (var i = 0; i < newHashes.Length - minLength; ++i)
            {
                _leftMap.Add(oldHashes.Length - 1);
                _rightMap.Add(oldHashes.Length);
            }
        }

        public void GetDiffs()
        {
            if (distance >= 0)
            {
                return;
            }

            distance = CalcV(out var x, out var y);
            CalcInsertsDeletes(x - y);
            CalcRemap();
            CalcLeftMap();
            CalcRightMap();

            // If the node is large and the diff is completely different, then fallback to naive remap
            if (oldHashes.Length > 10 && newHashes.Length > 10 && _remap.All(x => x == -1))
            {
                CalcNaiveRemap();
            }
        }

        public override string ToString()
        {
            return $"remap={remap.Dump()}, leftMap={leftMap.Dump()}, rightMap={rightMap.Dump()}";
        }
    }
}
