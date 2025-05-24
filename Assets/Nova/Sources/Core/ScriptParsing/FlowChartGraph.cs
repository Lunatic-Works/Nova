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
        private readonly Dictionary<string, FlowChartNode> endNodes = new Dictionary<string, FlowChartNode>();

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

        public void Clear()
        {
            CheckFreeze();
            nodes.Clear();
            startNodes.Clear();
            endNodes.Clear();
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

            if (HasNode(node))
            {
                Debug.LogWarning($"Nova: Overwrite node: {node.name}");
            }

            nodes[node.name] = node;
        }

        /// <summary>
        /// Get a node by name
        /// </summary>
        /// <param name="name">Name of the node</param>
        public FlowChartNode GetNode(string name)
        {
            if (!HasNode(name))
            {
                throw new ArgumentException($"Nova: Node {name} is not in the graph.");
            }

            return nodes[name];
        }

        /// <summary>
        /// Check if the graph contains the node with the given name
        /// </summary>
        /// <param name="name">Name of the node</param>
        public bool HasNode(string name)
        {
            return nodes.ContainsKey(name);
        }

        public bool HasNode(FlowChartNode node)
        {
            return HasNode(node.name);
        }

        public IEnumerable<string> GetStartNodeNames(StartNodeType type)
        {
            return startNodes.Values.Where(x => type.HasFlag(x.type)).Select(x => x.name);
        }

        private void CheckNode(FlowChartNode node)
        {
            CheckFreeze();

            if (!HasNode(node))
            {
                throw new ArgumentException($"Nova: Node {node.name} is not in the graph.");
            }
        }

        /// <summary>
        /// Add a start node
        /// </summary>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if the node is not in the graph.
        /// </exception>
        public void AddStart(FlowChartNode node, StartNodeType type)
        {
            CheckNode(node);
            startNodes[node.name] = new StartNode(node, type);
        }

        /// <summary>
        /// Check if the graph contains the given start node
        /// </summary>
        /// <param name="name">Name of the start node</param>
        public bool HasStart(string name)
        {
            return startNodes.ContainsKey(name);
        }

        public bool HasStart(FlowChartNode node)
        {
            return HasStart(node.name);
        }

        /// <summary>
        /// Add an end node
        /// </summary>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if the node is not in the graph.
        /// </exception>
        public void AddEnd(FlowChartNode node)
        {
            CheckNode(node);
            endNodes[node.name] = node;
        }

        /// <summary>
        /// Check if the graph contains the given end node
        /// </summary>
        /// <param name="name">Name of the end node</param>
        public bool HasEnd(string name)
        {
            return endNodes.ContainsKey(name);
        }

        public bool HasEnd(FlowChartNode node)
        {
            return HasEnd(node.name);
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
                    AddEnd(node);
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
