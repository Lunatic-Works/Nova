using System.Collections.Generic;
using Nova.Exceptions;
using UnityEngine.Assertions;

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
    /// <remarks>
    /// Everything in a node can not be modified after it is freezed
    /// </remarks>
    public class FlowChartNode
    {
        public FlowChartNode(string name, string description)
        {
            this.name = name;
            this.description = description;
        }

        /// <value>The name of the currenct flow chart node. The name should be unique for each nodes.</value>>
        public string name { get; private set; }

        /// <value>A short description of this flow chart node</value>
        public string description { get; private set; }

        /// <value>
        /// branches from current node
        /// </value>
        private readonly Dictionary<BranchInformation, FlowChartNode> branches =
            new Dictionary<BranchInformation, FlowChartNode>();

        private bool isFreezed = false;

        private FlowChartNodeType _type = FlowChartNodeType.Normal;

        private void CheckFreeze()
        {
            Assert.IsFalse(isFreezed, "Nova: Can NOT change the content of a node after its type is freezed");
        }

        /// <value>
        /// Type of this flow chart node. The value of this field is default to be normal
        /// </value>
        /// <remarks>
        /// The type of a node is always readable but only settable before its type if freezed.
        /// A flow chart tree should freeze all its nodes after construction.
        /// </remarks>
        public FlowChartNodeType type
        {
            get { return _type; }
            set
            {
                CheckFreeze();
                _type = value;
            }
        }

        /// <summary>
        /// Freeze the type of this node
        /// </summary>
        public void Freeze()
        {
            isFreezed = true;
        }

        /// <value>
        /// Dialogue entries in this node
        /// </value>
        private readonly List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

        /// <summary>
        /// Get the next node of a normal node. Only Normal nodes can call this property
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

                return branches[BranchInformation.Defualt];
            }
        }

        /// <summary>
        /// Add a branch to this node
        /// </summary>
        /// <param name="branchInformation">The information of the branch</param>
        /// <param name="nextNode">The next node at this branch</param>
        public void AddBranch(BranchInformation branchInformation, FlowChartNode nextNode)
        {
            CheckFreeze();
            branches.Add(branchInformation, nextNode);
        }

        /// <summary>
        /// Get all branches under this node
        /// </summary>
        /// <returns>All branch info of this node</returns>
        public IEnumerable<BranchInformation> GetAllBranches()
        {
            return branches.Keys;
        }

        /// <summary>
        /// Get next node by branch name
        /// </summary>
        /// <param name="branchName">
        /// The name of the branch. The name MUST represents one of its branches
        /// </param>
        /// <returns>
        /// The next node at the specified branch.
        /// </returns>
        public FlowChartNode GetNext(string branchName)
        {
            return branches[new BranchInformation(branchName)];
        }

        /// <summary>
        /// The number of branches
        /// </summary>
        public int BranchCount
        {
            get { return branches.Count; }
        }

        /// <summary>
        /// Add a dialogue entry to the end of the dialogue entry list
        /// </summary>
        /// <param name="entry"></param>
        public void AddDialogueEntry(DialogueEntry entry)
        {
            CheckFreeze();
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
        /// the number of dialogue entries in this node
        /// </summary>
        /// <value>
        /// The number of dialogue entries in this node
        /// </value>
        public int DialogueEntryCount
        {
            get { return dialogueEntries.Count; }
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