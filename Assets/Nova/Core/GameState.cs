using System;
using System.Collections.Generic;
using Nova.Exceptions;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    [System.Serializable]
    public class DialogueChangedEvent : UnityEvent<string>
    {
    }

    [System.Serializable]
    public class NodeChangedEvent : UnityEvent<string, string>
    {
    }

    [System.Serializable]
    public class BranchOccursEvent : UnityEvent<IEnumerable<BranchInformation>>
    {
    }

    [System.Serializable]
    public class CurrentRouteEndedEvent : UnityEvent<string>
    {
    }

    /// <inheritdoc />
    /// <summary>
    /// This class manages the AVG game state.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public string scriptPath;

        private readonly ScriptLoader scriptLoader = new ScriptLoader();
        private readonly AssetsLoader assetsLoader = new AssetsLoader();
        private FlowChartTree flowChartTree;

        private void Awake()
        {
            scriptLoader.Init(scriptPath);
            flowChartTree = scriptLoader.GetFlowChartTree();
        }

        #region status

        /// <summary>
        /// Nodes that has been walked through
        /// </summary>
        private List<string> walkedThroughNodes;

        /// <summary>
        /// The index of the current dialogue entry in the current node
        /// </summary>
        private int currentIndex;

        /// <summary>
        /// The current flow chart node
        /// </summary>
        private FlowChartNode currentNode;

        /// <summary>
        /// The current dialogueEntry
        /// </summary>
        private DialogueEntry currentDialogueEntry;

        #endregion

        /// <summary>
        /// This event will be triggered if the content of the dialogue has changed. New dialogue text will be
        /// sent to all listeners
        /// </summary>
        /// <remarks>
        /// The first parameter is the new dialogue text
        /// </remarks>
        public DialogueChangedEvent DialogueChanged;

        /// <summary>
        /// This event will be triggered if the node has changed. The name and discription of the new node will be
        /// sent to all listeners
        /// </summary>
        /// <remarks>
        /// The first parameter give the new node name, the second parameter give the new node description
        /// </remarks>
        public NodeChangedEvent NodeChanged;

        /// <summary>
        /// This event will be triggered if branches occur. The player has to choose which branch to take
        /// </summary>
        /// <remarks>
        /// The first parameter is an enumerable of BranchInformation 
        /// </remarks>
        public BranchOccursEvent BranchOccurs;

        /// <summary>
        /// This event will be triggered if the story reaches an end
        /// </summary>
        /// <remarks>
        /// The first parameter is the name of the end
        /// </remarks>
        public CurrentRouteEndedEvent CurrentRouteEnded;

        /// <summary>
        /// Called after the current node or the index of the current dialogue entry has changed.
        /// </summary>
        /// <remarks>
        /// The game state will be updated according to the current node and current dialogue index.
        /// This method will execute the action in the new current dialogue entry and informs all game state listeners
        /// Since the action inside the dialogue entry will be executed, this method should not be called twice
        /// if only one update has happen
        /// </remarks>
        private void UpdateGameState()
        {
            currentDialogueEntry = currentNode.GetDialogueEntryAt(currentIndex);
            currentDialogueEntry.ExecuteAction();
            DialogueChanged.Invoke(currentDialogueEntry.text);
        }

        /// <summary>
        /// Move on to the next node
        /// </summary>
        /// <param name="nextNode">The next node to move to</param>
        private void MoveToNode(FlowChartNode nextNode)
        {
            walkedThroughNodes.Add(nextNode.name);
            currentNode = nextNode;
            currentIndex = 0;
            NodeChanged.Invoke(currentNode.name, currentNode.description);
            UpdateGameState();
        }

        /// <summary>
        /// Start the game from the given node
        /// </summary>
        /// <param name="startNode">The node from where the game starts</param>
        private void GameStart(FlowChartNode startNode)
        {
            // clear possible history
            walkedThroughNodes = new List<string>();
            MoveToNode(startNode);
        }

        /// <summary>
        /// Start the game from the default start up point
        /// </summary>
        public void GameStart()
        {
            var startNode = flowChartTree.DefaultStartUpNode;
            GameStart(startNode);
        }

        /// <summary>
        /// Start the game from a named start point 
        /// </summary>
        /// <param name="startName">the name of the start</param>
        public void Gamestart(string startName)
        {
            var startNode = flowChartTree.GetStartUpNode(startName);
            GameStart(startNode);
        }

        private bool isBranching = false;

        /// <summary>
        /// Step to the next dialogue entry
        /// </summary>
        public void Step()
        {
            // if have a next dialogue entry in the current node, directly step to the next
            if (currentIndex + 1 < currentNode.DialogueEntryCount)
            {
                currentIndex += 1;
                UpdateGameState();
                return;
            }

            // Reach the end of a node, do something regards on the flow chart node type
            switch (currentNode.type)
            {
                case FlowChartNodeType.Normal:
                    // for normal node, just step directly to the next node
                    MoveToNode(currentNode.Next);
                    break;
                case FlowChartNodeType.Branching:
                    // A branch occurs, inform branch event listeners
                    if (isBranching)
                    {
                        // A branching is happening, but the player have not decided which branch to select yet
                        // Break directly to avoid duplicated invocations of the same branching event
                        break;
                    }

                    isBranching = true;
                    BranchOccurs.Invoke(currentNode.GetAllBranches());
                    break;
                case FlowChartNodeType.End:
                    CurrentRouteEnded.Invoke(flowChartTree.GetEndName(currentNode));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Call this method to choose a branch
        /// </summary>
        /// <remarks>
        /// This method should be called when a branch happends, Otherwise An InvalidAccessException will be raised
        /// </remarks>
        /// <param name="branchName">The name of the branch</param>
        /// <exception cref="InvalidAccessException">An InvalidAccessException will be thrown if this method
        /// is not called on branch happens</exception>
        public void SelectBranch(string branchName)
        {
            if (!isBranching)
            {
                throw new InvalidAccessException("Nova: Select branch should only be called when a branch happens");
            }

            isBranching = false;
            var nextNode = currentNode.GetNext(branchName);
            MoveToNode(nextNode);
        }
    }
}