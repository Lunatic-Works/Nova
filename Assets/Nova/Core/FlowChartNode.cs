using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nova.Exceptions;

namespace Nova
{
    public enum FlowChartNodeType
    {
        Normal,
        Branching,
        End
    }

    /// <summary>
    /// A node on the flow chart
    /// </summary>
    public class FlowChartNode
    {
        /// <value>The name of the currenct flow chart node. The name should be unique for each nodes.</value>>
        public string name;

        /// <value>A short description of this flow chart node</value>
        public string description;

        /// <value>
        /// branches from current node
        /// </value>
        public readonly Dictionary<BranchInformation, FlowChartNode> branches =
            new Dictionary<BranchInformation, FlowChartNode>();

        /// <value>
        /// Type of this flow chart node. The value of this field is default to be normal
        /// </value>
        public FlowChartNodeType type = FlowChartNodeType.Normal;

        /// <value>
        /// Dialogue entries in this node
        /// </value>
        private readonly List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

        /// <summary>
        /// Get the next node of a normal node. If the node has no succeedings, null will be returned.
        /// </summary>
        /// <exception cref="InvalidAccessException">
        /// An InvalidAccessException will be thrown if this node is not a Normal node
        /// </exception>
        public FlowChartNode Next
        {
            get
            {
                if (type != FlowChartNodeType.Normal)
                {
                    throw new InvalidAccessException(
                        "Nova: the Next field of a flow chart node is only avaliable when the node is of type Normal");
                }

                FlowChartNode next;
                branches.TryGetValue(BranchInformation.Defualt, out next);
                return next;
            }
        }

        /// <summary>
        /// Add a dialogue entry to the end of the dialogue entry list
        /// </summary>
        /// <param name="entry"></param>
        public void AddDialogueEntry(DialogueEntry entry)
        {
            dialogueEntries.Add(entry);
        }

        /// <summary>
        /// Get the dialogue entry at the given index
        /// </summary>
        /// <param name="index">the index of the element to be fetched</param>
        /// <returns>The dialogue entry at the given index</returns>
        public DialogueEntry GetDialogueEntryAt(int index)
        {
            return dialogueEntries[index];
        }

        /// <summary>
        /// Get the number of dialogue entries in this node
        /// </summary>
        /// <returns>
        /// The number of dialogue entries in this node
        /// </returns>
        public int GetDialogueEntryCount()
        {
            return dialogueEntries.Count;
        }

        // Two flow chart nodes are considered equal if they have the same name
        public override bool Equals(object obj)
        {
            var anotherObject = obj as FlowChartNode;
            return anotherObject != null && name.Equals(anotherObject.name);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}