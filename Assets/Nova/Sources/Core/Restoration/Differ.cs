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

        private ulong[] saveHashes, scriptHashes;
        // pseudo array of V[d][k]
        // the X value of furthest reached point for a d-path on diag-k
        private List<int> _V = new List<int>();

        // return V[d][k]
        private int V(int d, int k)
        {
            if (k > d || k < -d || ((k + d) % 2 != 0))
            {
                throw bug;
            }
            return _V[(d + 1) * d / 2 + (k + d) / 2];
        }

        public Differ(FlowChartNode node, IEnumerable<ReachedDialogueData> reachedData)
        {
            scriptHashes = node.GetAllDialogues().Select(x => x.textHash).ToArray();
            saveHashes = reachedData.Select(x => x.textHash).ToArray();

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
                    _V.Add(x);
                    if (x >= xMid && y >= yMax)
                    {
                        return d;
                    }
                }
            }
            throw bug;
        }

        public void GetDiffs(out List<int> deletes, out List<int> inserts)
        {
            deletes = new List<int>();
            inserts = new List<int>();

            int distance = CalcV(out var x, out var y);
            Debug.Log($"distance={distance}, x={x}, y={y}");
            int k = x - y;
            for (int d = distance - 1; d >= 0; d--)
            {
                if (k + 1 > d || (k - 1 >= -d && V(d, k + 1) < V(d, k - 1) + 1))
                {
                    k--;
                    x = V(d, k);
                    deletes.Add(x);
                }
                else
                {
                    k++;
                    x = V(d, k);
                    inserts.Add(x - k);
                }
            }
            deletes.Reverse();
            inserts.Reverse();
        }
    }
}
