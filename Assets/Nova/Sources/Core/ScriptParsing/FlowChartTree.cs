using System;
using System.Collections.Generic;
using System.Linq;
using Nova.Exceptions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// The data structure that stores the flow chart.
    /// </summary>
    /// <remarks>
    /// A well-defined flow chart tree should have at least one start node, and all nodes without children are
    /// marked as end nodes.
    /// Everything in a flow chart tree cannot be modified after it is frozen.
    /// </remarks>
    public class FlowChartTree
    {
        private readonly Dictionary<string, FlowChartNode> nodes = new Dictionary<string, FlowChartNode>();
        private readonly Dictionary<string, FlowChartNode> startNodes = new Dictionary<string, FlowChartNode>();
        private readonly Dictionary<string, FlowChartNode> unlockedStartNodes = new Dictionary<string, FlowChartNode>();
        private readonly Dictionary<FlowChartNode, string> endNodes = new Dictionary<FlowChartNode, string>();

        private bool isFrozen = false;

        /// <summary>
        /// Freeze all nodes. Should be called after the construction of the flow chart tree.
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
        /// Node to be added. No checking will be performed on the node name.
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
        /// <param name="name">Name of the node</param>
        /// <returns>The node if it is found, otherwise return null</returns>
        public FlowChartNode GetNode(string name)
        {
            nodes.TryGetValue(name, out var node);
            return node;
        }

        /// <summary>
        /// Check if the tree contains the node with the given name
        /// </summary>
        /// <param name="name">Name of the node</param>
        /// <returns>True if the tree contains the given node, otherwise return false</returns>
        public bool HasNode(string name)
        {
            return nodes.ContainsKey(name);
        }

        /// <summary>
        /// Check if the tree contains the given node
        /// </summary>
        /// <param name="node">Node to check</param>
        /// <returns>True if the tree contains the given node, otherwise return false</returns>
        public bool HasNode(FlowChartNode node)
        {
            return nodes.ContainsKey(node.name);
        }

        /// <summary>
        /// Returns names of all start nodes.
        /// </summary>
        public List<string> GetAllStartNodeNames()
        {
            return startNodes.Keys.ToList();
        }

        public List<string> GetAllUnlockedStartNodeNames()
        {
            return unlockedStartNodes.Keys.ToList();
        }

        /// <summary>
        /// Add a start node.
        /// </summary>
        /// <remarks>
        /// A name can be assigned to a start point, which can differ from the node name.
        /// The name should be unique among all start point names.
        /// This method will check if the given name is not in the tree, and the given node is already in the tree.
        /// </remarks>
        /// <param name="name">Name of the start point</param>
        /// <param name="node">The node to add</param>
        /// <exception cref="DuplicatedDefinitionException">
        /// DuplicatedDefinitionException will be thrown if the same start point name has been defined.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if the node is not in the tree.
        /// </exception>
        public void AddStart(string name, FlowChartNode node)
        {
            CheckFreeze();

            if (!HasNode(node))
            {
                throw new ArgumentException("Nova: Only node in the tree can be set as a start node.");
            }

            var existingStartNode = GetStartNode(name);
            if (existingStartNode != null && !existingStartNode.Equals(node))
            {
                throw new DuplicatedDefinitionException(
                    $"Nova: Duplicated definition of the same start name: {name}");
            }

            startNodes.Add(name, node);
        }

        public void AddUnlockedStart(string name, FlowChartNode node)
        {
            unlockedStartNodes.Add(name, node);
        }

        /// <summary>
        /// Get a start node by name
        /// </summary>
        /// <param name="name">Name of the start point</param>
        /// <returns>
        /// The start node if it is found, otherwise return null
        /// </returns>
        public FlowChartNode GetStartNode(string name)
        {
            startNodes.TryGetValue(name, out var node);
            return node;
        }

        /// <summary>
        /// Check if the tree contains the given start point name
        /// </summary>
        /// <param name="name">Name of the start point</param>
        /// <returns>True if the tree contains the given name, otherwise return false</returns>
        public bool HasStart(string name)
        {
            return startNodes.ContainsKey(name);
        }

        private FlowChartNode _defaultStartNode;

        /// <summary>
        /// The default start node.
        /// </summary>
        /// <remarks>
        /// When getting the value, if the default start node has been set, return the assigned value.
        /// Otherwise, check the start node dict. If there is at least one start node, return the first one.
        /// Otherwise, return null.
        /// When setting the value, this property will check if the default start node has already been set.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if two different nodes want to be the default start node.
        /// </exception>
        public FlowChartNode defaultStartNode
        {
            get
            {
                if (_defaultStartNode != null)
                {
                    return _defaultStartNode;
                }

                return startNodes.Values.FirstOrDefault();
            }
            set
            {
                CheckFreeze();

                if (_defaultStartNode == null)
                {
                    _defaultStartNode = value;
                }

                if (!_defaultStartNode.Equals(value))
                {
                    throw new ArgumentException("Nova: Only one node can be the default start point.");
                }
            }
        }

        /// <summary>
        /// Add an end node.
        /// </summary>
        /// <remarks>
        /// A name can be assigned to an end point, which can differ from the node name.
        /// The name should be unique among all end point names.
        /// This method will check if the given name is not in the tree, and the given node is already in the tree.
        /// </remarks>
        /// <param name="name">Name of the end point</param>
        /// <param name="node">The node to add</param>
        /// <exception cref="DuplicatedDefinitionException">
        /// DuplicatedDefinitionException will be thrown if the same end point name has been defined.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if the node is not in the tree.
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
                // The node has not been defined as an end
                if (endNodes.ContainsValue(name))
                {
                    // But the name has been used
                    throw new DuplicatedDefinitionException(
                        $"Nova: Duplicated definition of the same end name: {name}");
                }

                // The name is unique, add the node as en and
                endNodes.Add(node, name);
                return;
            }

            // The node has already been defined as an end
            if (existingNodeName != name)
            {
                // But the name of the end point is different
                throw new DuplicatedDefinitionException(
                    $"Nova: Assigning two different end name: {existingNodeName} and {name} to the same node.");
            }
        }

        /// <summary>
        /// Get the name of an end point
        /// </summary>
        /// <param name="node">The end node</param>
        /// <returns>
        /// The name of the end point if the node is an end node, otherwise return null
        /// </returns>
        public string GetEndName(FlowChartNode node)
        {
            return endNodes.TryGetValue(node, out var name) ? name : null;
        }

        /// <summary>
        /// Check if the tree contains the given end point name
        /// </summary>
        /// <param name="name">Name of the end point</param>
        /// <returns>True if the tree contains the given name, otherwise return false</returns>
        public bool HasEnd(string name)
        {
            return endNodes.ContainsValue(name);
        }

        /// <summary>
        /// Perform a sanity check on the flow chart tree.
        /// </summary>
        /// <remarks>
        /// The sanity check includes:
        /// + The tree has at least one start node;
        /// + All nodes without children are marked as end nodes.
        /// This method should be called after the construction of the flow chart tree.
        /// </remarks>
        public void SanityCheck()
        {
            CheckFreeze();

            if (startNodes.Count == 0)
            {
                throw new ArgumentException("Nova: At least one start node should exist.");
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