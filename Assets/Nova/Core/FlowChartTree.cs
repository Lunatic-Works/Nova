using System;
using System.Collections.Generic;

namespace Nova
{
    /// <summary>
    /// The data structure that stores the flow chart
    /// </summary>
    public class FlowChartTree
    {
        private readonly Dictionary<string, FlowChartNode> tree = new Dictionary<string, FlowChartNode>();

        private readonly Dictionary<string, FlowChartNode> startUpNodes = new Dictionary<string, FlowChartNode>();

        private FlowChartNode defaultStartUpNode;

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

        /// <summary>
        /// Check if the tree contains the given node
        /// </summary>
        /// <param name="node">the node to be found</param>
        /// <returns>true if the node has the given node</returns>
        public bool HasNode(FlowChartNode node)
        {
            return tree.ContainsKey(node.name);
        }

        /// <summary>
        /// Add a start up node
        /// </summary>
        /// <remarks>
        /// This method will check if the given node is already in the tree. it will raise an ArgumentException
        /// if the node is not found
        /// </remarks>
        /// <param name="name">the name of the start up</param>
        /// <param name="node">the start up node</param>
        public void AddStartUp(string name, FlowChartNode node)
        {
            if (!HasNode(node))
            {
                throw new ArgumentException("Only node in the tree can be setted as a start up node");
            }

            startUpNodes.Add(name, node);
        }

        /// <summary>
        /// Get a start up node by name
        /// </summary>
        /// <param name="name">
        /// the name of the entrance point. it should be noticed that the name of the entrance point may be differ from
        /// the name of the node
        /// </param>
        /// <returns>
        /// return the start up node with the given name. If the name is not found among the start up nodes,
        /// return null.
        /// </returns>
        public FlowChartNode GetStartUpNode(string name)
        {
            FlowChartNode node;
            startUpNodes.TryGetValue(name, out node);
            return node;
        }

        /// <summary>
        /// Check if a start up name has been registered
        /// </summary>
        /// <param name="name">The name of the start up</param>
        /// <returns>true if the name of the start up is registered</returns>
        public bool HasStartUp(string name)
        {
            return startUpNodes.ContainsKey(name);
        }

        /// <summary>
        /// Get the default start up node.
        /// </summary>
        /// <remarks>
        /// If the default start up node has been set, return the assigned value.
        /// If no default value has been set, check the start up node dict. If there is only one start up node,
        /// return that one, else return null.
        /// When setting new value to the default start up node, this property will check if the default start up
        /// node has already been set or not. If the a default value has already been set and is not equals to
        /// the new one, an ArgumentException will be raised.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// An ArgumentExecption will be raised if two different node whats to be the default start up node
        /// </exception>
        public FlowChartNode DefaultStartUpNode
        {
            get
            {
                if (defaultStartUpNode != null)
                {
                    return defaultStartUpNode;
                }

                if (startUpNodes.Count == 1)
                {
                    var e = startUpNodes.GetEnumerator();
                    e.MoveNext();
                    var node = e.Current.Value;
                    e.Dispose();
                    return node;
                }

                return null;
            }
            set
            {
                if (defaultStartUpNode == null)
                {
                    defaultStartUpNode = value;
                }

                if (!defaultStartUpNode.Equals(value))
                {
                    throw new ArgumentException("Nova: only one node can be the default start point.");
                }
            }
        }
    }
}