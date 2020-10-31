using System;
using System.Collections.Generic;
using Nova.Exceptions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// The data structure that stores the flow chart.
    /// </summary>
    /// <remarks>
    /// A well defined flow chart tree will have at least one start point and all nodes without childs are
    /// not marked as end. Everything in a flow chart tree cannot be modified after it is frozen
    /// </remarks>
    public class FlowChartTree
    {
        private readonly Dictionary<string, FlowChartNode> nodes = new Dictionary<string, FlowChartNode>();

        private readonly Dictionary<string, FlowChartNode> startUpNodes = new Dictionary<string, FlowChartNode>();

        private readonly Dictionary<FlowChartNode, string> endNodes = new Dictionary<FlowChartNode, string>();

        private bool isFrozen = false;

        /// <summary>
        /// Freeze all nodes. Should be called after the construction of this tree
        /// </summary>
        public void Freeze()
        {
            isFrozen = true;
            foreach (var node in nodes.Values)
            {
                node.Freeze();
            }
        }

        private void CheckFreeze()
        {
            Assert.IsFalse(isFrozen, "Nova: Cannot modify a flow chart tree when it is frozen.");
        }

        /// <summary>
        /// Add a node to the flow chart tree
        /// </summary>
        /// <param name="node">
        /// The node to be added. No checking will be performed on the name of the node
        /// </param>
        public void AddNode(FlowChartNode node)
        {
            CheckFreeze();
            var name = node.name;
            nodes.Add(name, node);
        }

        /// <summary>
        /// Get a node by name
        /// </summary>
        /// <param name="name">the name of the tree node</param>
        /// <returns>The specified node if the node with the given name is found, otherwise return null</returns>
        public FlowChartNode GetNode(string name)
        {
            nodes.TryGetValue(name, out var node);
            return node;
        }

        /// <summary>
        /// Check if the tree have the node with the given name
        /// </summary>
        /// <param name="name">The name to be found</param>
        /// <returns>true if the tree contains the given node, else return false</returns>
        public bool HasNode(string name)
        {
            return nodes.ContainsKey(name);
        }

        /// <summary>
        /// Check if the tree contains the given node
        /// </summary>
        /// <param name="node">the node to be found</param>
        /// <returns>true if the node has the given node</returns>
        public bool HasNode(FlowChartNode node)
        {
            return nodes.ContainsKey(node.name);
        }

        /// <summary>
        /// Returns names of all startup nodes.
        /// </summary>
        public string[] GetAllStartupNodeNames()
        {
            string[] names = new string[startUpNodes.Count];
            startUpNodes.Keys.CopyTo(names, 0);
            return names;
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
            CheckFreeze();

            if (!HasNode(node))
            {
                throw new ArgumentException("Nova: Only node in the tree can be set as a start up node.");
            }

            var existingStartNode = GetStartUpNode(name);
            if (existingStartNode != null && !existingStartNode.Equals(node))
            {
                throw new DuplicatedDefinitionException(
                    $"Nova: Duplicated definition of the same start up name: {name}");
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
            startUpNodes.TryGetValue(name, out var node);
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

        private FlowChartNode _defaultStartUpNode;

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
        /// An ArgumentException will be raised if two different node whats to be the default start up node
        /// </exception>
        public FlowChartNode defaultStartUpNode
        {
            get
            {
                if (_defaultStartUpNode != null)
                {
                    return _defaultStartUpNode;
                }

                if (startUpNodes.Count == 0) return null;

                var e = startUpNodes.GetEnumerator();
                e.MoveNext();
                var node = e.Current.Value;
                e.Dispose();
                return node;
            }
            set
            {
                CheckFreeze();

                if (_defaultStartUpNode == null)
                {
                    _defaultStartUpNode = value;
                }

                if (!_defaultStartUpNode.Equals(value))
                {
                    throw new ArgumentException("Nova: Only one node can be the default start point.");
                }
            }
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
            CheckFreeze();

            if (!HasNode(node))
            {
                throw new ArgumentException("Nova: Only node in the tree can be set as an end node.");
            }

            var existingNodeName = GetEndName(node);
            if (existingNodeName == null)
            {
                // This node has not been defined as an end
                if (endNodes.ContainsValue(name))
                {
                    // but the name has been used
                    throw new DuplicatedDefinitionException(
                        $"Nova: Duplicated definition of the same end name: {name}");
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
                    $"Nova: Assigning two different end name: {existingNodeName} and {name} to the same node.");
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
            var hasFound = endNodes.TryGetValue(node, out var name);
            return hasFound ? name : null;
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
            CheckFreeze();

            if (startUpNodes.Count == 0)
            {
                throw new ArgumentException("Nova: At least one start up should exist.");
            }

            foreach (var node in nodes.Values)
            {
                if (node.branchCount == 0 && node.type != FlowChartNodeType.End)
                {
                    Debug.LogWarningFormat(
                        "Nova: Node {0} has no child. It will be marked as an end with name {0}.",
                        node.name);
                    node.type = FlowChartNodeType.End;
                    AddEnd(node.name, node);
                }
            }
        }
    }
}