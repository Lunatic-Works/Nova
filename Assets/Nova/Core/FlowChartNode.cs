using System.Collections.Generic;
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
        /// Dialogue entries in this node
        /// </value>
        public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

        /// <value>
        /// Type of this flow chart node. The value of this field is default to be normal
        /// </value>
        public FlowChartNodeType type = FlowChartNodeType.Normal;

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