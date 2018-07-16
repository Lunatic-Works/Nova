using System.Collections.Generic;

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
        public Dictionary<BranchInformation, FlowChartNode> branches =
            new Dictionary<BranchInformation, FlowChartNode>();

        /// <value>
        /// Dialogue entries in this node
        /// </value>
        public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

        /// <value>
        /// Type of this flow chart node. The value of this field if default to be normal
        /// </value>
        public FlowChartNodeType type = FlowChartNodeType.Normal;

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