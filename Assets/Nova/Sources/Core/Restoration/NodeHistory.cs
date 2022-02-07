using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nova
{
    // TODO: use a radix tree to store node histories
    [Serializable]
    public class NodeHistoryData
    {
        public readonly IReadOnlyList<string> nodeNames;
        public readonly IReadOnlyDictionary<int, IReadOnlyDictionary<int, ulong>> interrupts;

        public NodeHistoryData(NodeHistory nodeHistory)
        {
            nodeNames = nodeHistory.Select(x => x.Key).ToList();
            interrupts = nodeHistory.interrupts.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyDictionary<int, ulong>)pair.Value.ToDictionary(
                    pair2 => pair2.Key,
                    pair2 => pair2.Value
                )
            );
        }
    }

    public class NodeHistory : CountedHashableList<string>
    {
        // Node history index -> dialogue index -> variables hash
        public readonly SortedDictionary<int, SortedDictionary<int, ulong>> interrupts =
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

                    var first = true;
                    foreach (var pair2 in pair.Value)
                    {
                        if (nodeHistoryIndex == index + count - 1 && pair2.Key >= dialogueCount)
                        {
                            break;
                        }

                        // Add nodeHistoryIndex to the hash only if there is any pair2.Key < dialogueCount
                        if (first)
                        {
                            first = false;
                            x += (ulong)nodeHistoryIndex;
                            x *= 11400714819323199563UL;
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
                if (index < dialogueIndex)
                {
                    continue;
                }

                dict.Remove(index);
            }

            if (dict.Count <= 0)
            {
                interrupts.Remove(interrupts.Keys.Last());
            }

            needCalculateHash = true;
        }

        public override void Clear()
        {
            base.Clear();
            interrupts.Clear();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("NodeHistory: ");
            sb.Append(string.Join(", ", from pair in list select $"{pair.Key}:{pair.Value}"));
            sb.Append(", interrupts: {");
            var first = true;
            foreach (var pair in interrupts)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append($"{pair.Key}: {{");
                sb.Append(string.Join(", ", from pair2 in pair.Value select $"{pair2.Key}:{pair2.Value}"));
                sb.Append("}");
            }

            sb.Append("}");
            return sb.ToString();
        }
    }
}