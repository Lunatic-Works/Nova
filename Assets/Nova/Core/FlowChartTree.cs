using System;
using System.Collections.Generic;
using Nova.Exceptions;
using UnityEditor;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// The data structure that stores the flow chart.
    /// </summary>
    /// <remarks>
    /// A well defined flow chart tree will have at least one start point and all nodes without childs are
    /// not marked as end
    /// </remarks>
    public class FlowChartTree
    {
        private readonly Dictionary<string, FlowChartNode> tree = new Dictionary<string, FlowChartNode>();

        private readonly Dictionary<string, FlowChartNode> startUpNodes = new Dictionary<string, FlowChartNode>();

        private readonly Dictionary<FlowChartNode, string> endNodes = new Dictionary<FlowChartNode, string>();

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
        /// if the node is not found. If the name has already been defined before, a DuplicatedDefinitionException
        /// will been thrown
        /// </remarks>
        /// <param name="name">
        /// the name of the start up. the name of the starting point can be different from that of the node
        /// </param>
        /// <param name="node">the start up node</param>
        /// <exception cref="DuplicatedDefinitionException">
        /// If the same name has been defined for multiple times, A DuplicatedDefinitionException will been thrown
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException will be raised if the node is not in the tree
        /// </exception>
        public void AddStartUp(string name, FlowChartNode node)
        {
            if (!HasNode(node))
            {
                throw new ArgumentException("Nova: Only node in the tree can be setted as a start up node");
            }

            var existingStartNode = GetStartUpNode(name);
            if (existingStartNode != null && !existingStartNode.Equals(node))
            {
                throw new DuplicatedDefinitionException(
                    string.Format("Nova: duplicated definition of the same start up name: {0}", name));
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

        /// <summary>
        /// Check if the flow chart tree has an end with the given name
        /// </summary>
        /// <param name="name">the name of the end</param>
        /// <returns>true if an end with the given name is found, else return false</returns>
        public bool HasEnd(string name)
        {
            return endNodes.ContainsValue(name);
        }

        /// <summary>
        /// Add an end node.
        /// </summary>
        /// <remarks>
        /// This method will check if the given node is already in the tree. it will raise an ArgumentException
        /// if the node is not found. A node can have only one end name, and an end name can refer to only one node.
        /// A DuplicatedDefinitionException will been raised of the above bijection rule is violated. 
        /// </remarks>
        /// <param name="name">
        /// the name of the end. the name of the end can be different from that of the node
        /// </param>
        /// <param name="node">the end node</param>
        /// <exception cref="ArgumentException">
        /// An ArgumentException will be raised if the node if not in the tree
        /// </exception>
        public void AddEnd(string name, FlowChartNode node)
        {
            if (!HasNode(node))
            {
                throw new ArgumentException("Nova: Only node in the tree can be setted as an end node");
            }

            var existingNodeName = GetEndName(node);
            if (existingNodeName == null)
            {
                // This node has not been defined as an end
                if (endNodes.ContainsValue(name))
                {
                    // but the name has been used
                    throw new DuplicatedDefinitionException(
                        string.Format("Nova: duplicated definition of the same end name {0}", name));
                }

                // The name is legal, add end node
                endNodes.Add(node, name);
                return;
            }

            // This node has already been defined
            if (existingNodeName != name)
            {
                // But the end name of this node is not the same as the current one
                throw new DuplicatedDefinitionException(
                    string.Format("Nova: assign two different end name: {0} and {1} to the same node",
                        existingNodeName, name));
            }
        }

        /// <summary>
        /// Get the name of the end node
        /// </summary>
        /// <param name="node">The end node</param>
        /// <returns>
        /// the name of the end if the node is an end node, null will been returned if the node is not an end.
        /// </returns>
        public string GetEndName(FlowChartNode node)
        {
            string name;
            var hasFound = endNodes.TryGetValue(node, out name);
            return hasFound ? name : null;
        }

        /// <summary>
        /// Perform a sanity check on the flow char tree
        /// </summary>
        /// <remarks>
        /// The sanity check includes:
        /// + Whether the flow chart tree have a start point
        /// + If all the nodes that have no child node are marked as type End
        /// This method should be called after the construction of the flow chart tree
        /// </remarks>
        public void SanityCheck()
        {
            if (startUpNodes.Count == 0)
            {
                throw new ArgumentException("Nova: At least one start up should exists");
            }

            foreach (var node in tree.Values)
            {
                if (node.branches.Count == 0 && node.type != FlowChartNodeType.End)
                {
                    Debug.Log(string.Format(
                        "<color=red>Nova: Node {0} has no childs. It will be marked as an end with name {0}</color>",
                        node.name));
                    node.type = FlowChartNodeType.End;
                    AddEnd(node.name, node);
                }
            }
        }
    }
}