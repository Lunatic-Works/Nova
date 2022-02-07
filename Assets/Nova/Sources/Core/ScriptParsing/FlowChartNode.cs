using Nova.Exceptions;
using System.Collections.Generic;
using UnityEngine;

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
    /// Everything in a node cannot be modified after it is frozen
    /// </remarks>
    public class FlowChartNode
    {
        /// <summary>
        /// Internally used name of the flow chart node.
        /// The name should be unique for each node.
        /// </summary>
        public readonly string name;

        public FlowChartNode(string name)
        {
            this.name = name;
        }

        private bool isFrozen = false;

        /// <summary>
        /// Freeze the type of this node
        /// </summary>
        public void Freeze()
        {
            isFrozen = true;
        }

        public void Unfreeze()
        {
            isFrozen = false;
        }

        private void CheckFreeze()
        {
            Utils.RuntimeAssert(!isFrozen, "Cannot modify a flow chart node when it is frozen.");
        }

        private FlowChartNodeType _type = FlowChartNodeType.Normal;

        /// <summary>
        /// Type of this flow chart node. Defaults to Normal.
        /// </summary>
        /// <remarks>
        /// The type of a node is always gettable but only settable before it is frozen.
        /// A flow chart tree should freeze all its nodes after the construction.
        /// </remarks>
        public FlowChartNodeType type
        {
            get => _type;
            set
            {
                CheckFreeze();
                _type = value;
            }
        }

        #region Displayed names

        /// <summary>
        /// Displayed node name in each locale.
        /// </summary>
        private readonly Dictionary<SystemLanguage, string> _displayNames = new Dictionary<SystemLanguage, string>();
        public IReadOnlyDictionary<SystemLanguage, string> displayNames => _displayNames;

        public void AddLocalizedName(SystemLanguage locale, string displayName)
        {
            CheckFreeze();
            _displayNames[locale] = displayName;
        }

        #endregion

        #region Dialogue entries

        /// <summary>
        /// Dialogue entries in this node.
        /// </summary>
        private IReadOnlyList<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

        public int dialogueEntryCount => dialogueEntries.Count;

        public void SetDialogueEntries(IReadOnlyList<DialogueEntry> entries)
        {
            CheckFreeze();
            dialogueEntries = entries;
        }

        public void AddLocalizedDialogueEntries(SystemLanguage locale, IReadOnlyList<LocalizedDialogueEntry> entries)
        {
            Utils.RuntimeAssert(entries.Count == dialogueEntries.Count, "Localized dialogue entry count differs.");
            CheckFreeze();
            for (int i = 0; i < entries.Count; ++i)
            {
                dialogueEntries[i].AddLocalized(locale, entries[i]);
            }
        }

        /// <summary>
        /// Get the dialogue entry at the given index
        /// </summary>
        /// <param name="index">The index of the element to be fetched</param>
        /// <returns>The dialogue entry at the given index</returns>
        public DialogueEntry GetDialogueEntryAt(int index)
        {
            return dialogueEntries[index];
        }

        #endregion

        #region Branches

        /// <summary>
        /// Branches in this node
        /// </summary>
        private readonly Dictionary<BranchInformation, FlowChartNode> branches =
            new Dictionary<BranchInformation, FlowChartNode>();

        /// <summary>
        /// The number of branches
        /// </summary>
        public int branchCount => branches.Count;

        /// <summary>
        /// Get the next node of a normal node. Only Normal nodes can call this property
        /// </summary>
        /// <exception cref="InvalidAccessException">
        /// An InvalidAccessException will be thrown if this node is not a Normal node
        /// </exception>
        public FlowChartNode next
        {
            get
            {
                if (type != FlowChartNodeType.Normal)
                {
                    throw new InvalidAccessException(
                        "Nova: Field Next of a flow chart node is only available when its type is Normal.");
                }

                return branches[BranchInformation.Default];
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
        /// The name of the branch.
        /// </param>
        /// <returns>
        /// The next node at the specified branch.
        /// </returns>
        public FlowChartNode GetNext(string branchName)
        {
            return branches[new BranchInformation(branchName)];
        }

        #endregion

        // FlowChartNode are considered equal if they have the same name
        public override bool Equals(object obj)
        {
            return obj is FlowChartNode other && name == other.name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}