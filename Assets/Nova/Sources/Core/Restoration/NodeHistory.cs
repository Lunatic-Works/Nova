using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova
{
    // TODO: use a radix tree to store node histories
    [Serializable]
    public class NodeHistoryData
    {
        public readonly List<string> nodeNames;
        public readonly SortedDictionary<int, SortedDictionary<int, ulong>> interrupts;

        public NodeHistoryData(NodeHistory nodeHistory)
        {
            nodeNames = nodeHistory.Select(x => x.Key).ToList();
            interrupts = new SortedDictionary<int, SortedDictionary<int, ulong>>(nodeHistory.interrupts);
        }
    }

    public class NodeHistory : CountedHashableList<string>
    {
        // Node history index -> dialogue index -> variables hash
        public SortedDictionary<int, SortedDictionary<int, ulong>> interrupts =
            new SortedDictionary<int, SortedDictionary<int, ulong>>();

        // Knuth's golden ratio multiplicative hashing
        public override ulong GetHashULong(int index, int count)
        {
            unchecked
            {
                var x = base.GetHashULong(index, count);
                foreach (var pair in interrupts)
                {
                    var nodeHistoryIndex = pair.Key;
                    if (nodeHistoryIndex < index)
                    {
                        continue;
                    }

                    if (nodeHistoryIndex >= index + count)
                    {
                        break;
                    }

                    foreach (var hash in pair.Value.Values)
                    {
                        x += hash;
                        x *= 11400714819323199563UL;
                    }
                }

                return x;
            }
        }

        public ulong GetHashULong(int index, int count, int dialogueCount)
        {
            unchecked
            {
                var x = base.GetHashULong(index, count);
                foreach (var pair in interrupts)
                {
                    var nodeHistoryIndex = pair.Key;
                    if (nodeHistoryIndex < index)
                    {
                        continue;
                    }

                    if (nodeHistoryIndex >= index + count)
                    {
                        break;
                    }

                    x += (ulong)nodeHistoryIndex;
                    x *= 11400714819323199563UL;

                    foreach (var pair2 in pair.Value)
                    {
                        if (nodeHistoryIndex == index + count - 1 && pair2.Key >= dialogueCount)
                        {
                            break;
                        }

                        x += (ulong)pair2.Key;
                        x *= 11400714819323199563UL;
                        x += pair2.Value;
                        x *= 11400714819323199563UL;
                    }
                }

                return x;
            }
        }

        public void AddInterrupt(int dialogueIndex, Variables variables)
        {
            interrupts.Ensure(list.Count - 1)[dialogueIndex] = variables.hash;
            needCalculateHash = true;
        }

        public override void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
            foreach (var nodeHistoryIndex in interrupts.Keys.ToList())
            {
                if (nodeHistoryIndex < index)
                {
                    continue;
                }

                if (nodeHistoryIndex >= index + count)
                {
                    break;
                }

                interrupts.Remove(nodeHistoryIndex);
            }
        }

        // The interrupt at dialogueIndex is also removed, because NodeHistory saves the state before the checkpoint
        public void RemoveInterruptsAfter(int dialogueIndex)
        {
            var dict = interrupts.Values.LastOrDefault();
            if (dict == null)
            {
                return;
            }

            foreach (var index in dict.Keys.ToList())
            {
                if (index >= dialogueIndex)
                {
                    break;
                }

                dict.Remove(index);
            }

            needCalculateHash = true;
        }

        public override void Clear()
        {
            base.Clear();
            interrupts.Clear();
        }
    }
}