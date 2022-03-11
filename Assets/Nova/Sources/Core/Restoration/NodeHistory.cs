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
            return GetHashULong(index, count, int.MaxValue);
        }

        public ulong GetHashULong(int index, int count, int dialogueCount)
        {
            unchecked
            {
                var x = 0UL;
                for (var i = index; i < index + count; ++i)
                {
                    x += (ulong)list[i].Key.GetHashCode();
                    x *= 11400714819323199563UL;

                    if (interrupts.TryGetValue(i, out var dict))
                    {
                        foreach (var pair in dict)
                        {
                            if (i == index + count - 1 && pair.Key >= dialogueCount)
                            {
                                break;
                            }

                            x += (ulong)pair.Key;
                            x *= 11400714819323199563UL;
                            x += pair.Value;
                            x *= 11400714819323199563UL;
                        }
                    }
                }

                return x;
            }
        }

        private void UpdateHashULong(int dialogueIndex, ulong variablesHash)
        {
            unchecked
            {
                _hash += (ulong)dialogueIndex;
                _hash *= 11400714819323199563UL;
                _hash += (ulong)variablesHash;
                _hash *= 11400714819323199563UL;
            }
        }

        public void AddInterrupt(int dialogueIndex, Variables variables)
        {
            interrupts.Ensure(list.Count - 1)[dialogueIndex] = variables.hash;
            if (!needCalculateHash)
            {
                UpdateHashULong(dialogueIndex, variables.hash);
            }
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

        public void RemoveInterruptsAfter(int nodeHistoryIndex, int dialogueIndex)
        {
            if (interrupts.Count <= 0)
            {
                return;
            }

            var pair = interrupts.Last();
            if (pair.Key != nodeHistoryIndex)
            {
                return;
            }

            var dict = pair.Value;
            foreach (var index in dict.Keys.ToList())
            {
                // The interrupt at dialogueIndex is also removed, because NodeHistory saves the state before the checkpoint
                if (index < dialogueIndex)
                {
                    continue;
                }

                dict.Remove(index);
            }

            if (dict.Count <= 0)
            {
                interrupts.Remove(pair.Key);
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