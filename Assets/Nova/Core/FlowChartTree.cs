using System.Collections.Generic;

namespace Nova
{
    /// <summary>
    /// The data structure that stores the flow chart
    /// </summary>
    public class FlowChartTree
    {
        private readonly Dictionary<string, FlowChartNode> tree = new Dictionary<string, FlowChartNode>();

        /// <summary>
        /// Add a node to the flow chart tree
        /// </summary>
        /// <param name="node">
        /// The node to be added. No checking will be performed on the name of the node
        /// </param>
        public void AddNode(FlowChartNode node)
        {
            var name = node.name;
            tree.Add(name, node);
        }

        /// <summary>
        /// Find the node by name
        /// </summary>
        /// <param name="name">the name of the tree node</param>
        /// <returns>The specified node if the node with the given name is found, otherwise return null</returns>
        public FlowChartNode FindNode(string name)
        {
            FlowChartNode node;
            tree.TryGetValue(name, out node);
            return node;
        }

        /// <summary>
        /// Check if the tree have the node with the given name
        /// </summary>
        /// <param name="name">The name to be found</param>
        /// <returns>true if the tree contains the given node, else return false</returns>
        public bool HasNode(string name)
        {
            return tree.ContainsKey(name);
        }
    }
}