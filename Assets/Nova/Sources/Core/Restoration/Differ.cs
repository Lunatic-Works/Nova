using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    // An O(ND) diff algorithm: http://www.xmailserver.org/diff2.pdf
    class Differ
    {
        private static readonly Exception bug = new Exception("differ internal bug");

        private readonly ulong[] saveHashes, scriptHashes;
        // pseudo array of V[d][k]
        // the X value of furthest reached point for a d-path on diag-k
        private readonly List<int> v = new List<int>();
        private readonly List<int> _inserts = new List<int>();
        private readonly List<int> _deletes = new List<int>();
        private readonly List<int> _remap = new List<int>();

        public IReadOnlyList<int> inserts => _inserts;
        public IReadOnlyList<int> deletes => _deletes;
        // remap[i] == j means old list index i maps to new list index j
        // new item in new list remaps to index -1
        public IReadOnlyList<int> remap => _remap;
        public int distance { get; private set; }

        // return V[d][k]
        private int V(int d, int k)
        {
            if (k > d || k < -d || ((k + d) % 2 != 0))
            {
                throw bug;
            }
            return v[(d + 1) * d / 2 + (k + d) / 2];
        }

        public Differ(FlowChartNode node, IEnumerable<ReachedDialogueData> reachedData)
        {
            scriptHashes = node.GetAllDialogues().Select(x => x.textHash).ToArray();
            saveHashes = reachedData.Select(x => x.textHash).ToArray();
            distance = -1;

            Debug.Log($"{scriptHashes.Select(x => x % 32).Dump()} vs {saveHashes.Select(x => x % 32).Dump()}");
        }

        // find the shortest path and calculate V array
        // return the path length and the end point
        private int CalcV(out int x, out int y)
        {
            var xMax = saveHashes.Length + scriptHashes.Length;
            var xMid = saveHashes.Length;
            var yMax = scriptHashes.Length;
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
                    while (x < xMax && y < yMax && (x >= xMid || saveHashes[x] == scriptHashes[y]))
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
            throw bug;
        }

        private void CalcInsertsDeletes(int x, int k)
        {
            for (int d = distance - 1; d >= 0; d--)
            {
                if (k + 1 > d || (k - 1 >= -d && V(d, k + 1) < V(d, k - 1) + 1))
                {
                    k--;
                    x = V(d, k);
                    _deletes.Add(x);
                }
                else
                {
                    k++;
                    x = V(d, k);
                    _inserts.Add(x - k);
                }
            }
            _deletes.Reverse();
            _inserts.Reverse();
        }

        private void CalcRemap()
        {
            int x = 0, i = 0, j = 0;
            for (var y = 0; y < scriptHashes.Length; y++)
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
                    _remap.Add(x);
                    x++;
                }
            }
        }

        public void GetDiffs()
        {
            if (distance >= 0)
            {
                return;
            }

            distance = CalcV(out var x, out var y);
            Debug.Log($"distance={distance}, x={x}, y={y}");
            CalcInsertsDeletes(x, x - y);
            CalcRemap();
        }
    }
}
