using Nova.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [Flags]
    public enum StartNodeType
    {
        None = 0,
        Locked = 1,
        Unlocked = 2,
        Debug = 4,
        Normal = Locked | Unlocked,
        All = Locked | Unlocked | Debug
    }

    public class StartNode
    {
        public readonly FlowChartNode node;
        public readonly StartNodeType type;

        public string name => node.name;

        public StartNode(FlowChartNode node, StartNodeType type)
        {
            this.node = node;
            this.type = type;
        }
    }

    /// <summary>
    /// The data structure that stores the flow chart.
    /// </summary>
    /// <remarks>
    /// A well-defined flow chart graph should have at least one start node, and all nodes without children are
    /// marked as end nodes.
    /// Everything in a flow chart graph cannot be modified after it is frozen.
    /// </remarks>
    public class FlowChartGraph : IEnumerable<FlowChartNode>
    {
        private readonly Dictionary<string, FlowChartNode> nodes = new Dictionary<string, FlowChartNode>();
        private readonly Dictionary<string, StartNode> startNodes = new Dictionary<string, StartNode>();
        private readonly Dictionary<FlowChartNode, string> endNodes = new Dictionary<FlowChartNode, string>();

        private bool isFrozen;

        /// <summary>
        /// Freeze all nodes. Should be called after the construction of the flow chart graph.
        /// </summary>
        public void Freeze()
        {
            isFrozen = true;
            foreach (var node in nodes.Values)
            {
                node.Freeze();
            }
        }

        public void Unfreeze()
        {
            isFrozen = false;
            foreach (var node in nodes.Values)
            {
                node.Unfreeze();
            }
        }

        private void CheckFreeze()
        {
            Utils.RuntimeAssert(!isFrozen, "Cannot modify a flow chart graph when it is frozen.");
        }

        /// <summary>
        /// Add a node to the flow chart graph
        /// </summary>
        /// <param name="node">The node to add</param>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if the name is null or empty.
        /// </exception>
        public void AddNode(FlowChartNode node)
        {
            CheckFreeze();

            if (string.IsNullOrEmpty(node.name))
            {
                throw new ArgumentException("Nova: Node name is null or empty.");
            }

            if (nodes.ContainsKey(node.name))
            {
                Debug.LogWarning($"Nova: Overwrite node: {node.name}");
            }

            nodes[node.name] = node;
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
        /// Check if the graph contains the node with the given name
        /// </summary>
        /// <param name="name">Name of the node</param>
        /// <returns>True if the graph contains the given node, otherwise return false</returns>
        public bool HasNode(string name)
        {
            return nodes.ContainsKey(name);
        }

        /// <summary>
        /// Check if the graph contains the given node
        /// </summary>
        /// <param name="node">Node to check</param>
        /// <returns>True if the graph contains the given node, otherwise return false</returns>
        public bool HasNode(FlowChartNode node)
        {
            return nodes.ContainsKey(node.name);
        }

        public IEnumerable<string> GetStartNodeNames(StartNodeType type)
        {
            return startNodes.Values.Where(x => type.HasFlag(x.type)).Select(x => x.name);
        }

        /// <summary>
        /// Add a start node.
        /// </summary>
        /// <remarks>
        /// A name can be assigned to a start point, which can differ from the node name.
        /// The name should be unique among all start point names.
        /// This method will check if the given name is not in the graph, and the given node is already in the graph.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if the name is null or empty, or the node is not in the graph.
        /// </exception>
        public void AddStart(FlowChartNode node, StartNodeType type)
        {
            CheckFreeze();

            if (!HasNode(node))
            {
                throw new ArgumentException("Nova: Only node in the graph can be set as a start node.");
            }

            if (startNodes.ContainsKey(node.name))
            {
                Debug.LogWarning($"Nova: Overwrite start point: {node.name}");
            }

            startNodes[node.name] = new StartNode(node, type);
        }

        /// <summary>
        /// Check if the graph contains the given start point name
        /// </summary>
        /// <param name="name">Name of the start point</param>
        /// <returns>True if the graph contains the given name, otherwise return false</returns>
        public bool HasStart(string name)
        {
            return startNodes.ContainsKey(name);
        }

        /// <summary>
        /// Add an end node.
        /// </summary>
        /// <remarks>
        /// A name can be assigned to an end point, which can differ from the node name.
        /// The name should be unique among all end point names.
        /// This method will check if the given name is not in the graph, and the given node is already in the graph.
        /// </remarks>
        /// <param name="name">Name of the end point</param>
        /// <param name="node">The node to add</param>
        /// <exception cref="DuplicatedDefinitionException">
        /// DuplicatedDefinitionException will be thrown if assigning two different end names to the same node.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if the name is null or empty, or the node is not in the graph.
        /// </exception>
        public void AddEnd(string name, FlowChartNode node)
        {
            CheckFreeze();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Nova: End name is null or empty.");
            }

            if (!HasNode(node))
            {
                throw new ArgumentException("Nova: Only node in the graph can be set as an end node.");
            }

            var existingNodeName = GetEndName(node);
            if (existingNodeName == null)
            {
                // The node has not been defined as an end
                if (endNodes.ContainsValue(name))
                {
                    // But the name has been used
                    Debug.LogWarning($"Nova: Overwrite end point: {name}");
                }

                // The name is unique, add the node as en and
                endNodes[node] = name;
                return;
            }

            // The node has already been defined as an end
            if (existingNodeName != name)
            {
                // But the name of the end point is different
                throw new DuplicatedDefinitionException(
                    $"Nova: Assigning two different end names to the same node: {existingNodeName} and {name}");
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
            endNodes.TryGetValue(node, out var name);
            return name;
        }

        /// <summary>
        /// Check if the graph contains the given end point name
        /// </summary>
        /// <param name="name">Name of the end point</param>
        /// <returns>True if the graph contains the given name, otherwise return false</returns>
        public bool HasEnd(string name)
        {
            return endNodes.ContainsValue(name);
        }

        /// <summary>
        /// Perform a sanity check on the flow chart graph.
        /// </summary>
        /// <remarks>
        /// The sanity check includes:
        /// + The graph has at least one start node;
        /// + All nodes without children are marked as end nodes.
        /// This method should be called after the construction of the flow chart graph.
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
                    Debug.LogWarning(
                        $"Nova: Node {node.name} has no child. It will be marked as an end with name {node.name}.");
                    node.type = FlowChartNodeType.End;
                    AddEnd(node.name, node);
                }
            }
        }

        public IEnumerator<FlowChartNode> GetEnumerator()
        {
            return nodes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return nodes.Values.GetEnumerator();
        }
    }
}
