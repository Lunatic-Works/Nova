using System;
using System.Collections.Generic;
using System.Linq;
using Nova.Exceptions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Nova
{
    #region event types and event datas

    public class DialogueChangedEventData
    {
        public DialogueChangedEventData(string labelName, int dialogueIndex, string text,
            IEnumerable<string> voicesForNextDialogue)
        {
            this.labelName = labelName;
            this.dialogueIndex = dialogueIndex;
            this.text = text;
            this.voicesForNextDialogue = voicesForNextDialogue;
        }

        public string labelName { get; private set; }
        public int dialogueIndex { get; private set; }
        public string text { get; private set; }

        public IEnumerable<string> voicesForNextDialogue { get; private set; }
    }

    [System.Serializable]
    public class DialogueChangedEvent : UnityEvent<DialogueChangedEventData>
    {
    }

    public class NodeChangedEventData
    {
        public NodeChangedEventData(string nodeName, string nodeDescription)
        {
            this.nodeName = nodeName;
            this.nodeDescription = nodeDescription;
        }

        public string nodeName { get; private set; }
        public string nodeDescription { get; private set; }
    }

    [System.Serializable]
    public class NodeChangedEvent : UnityEvent<NodeChangedEventData>
    {
    }

    public class BranchOccursEventData
    {
        public BranchOccursEventData(IEnumerable<BranchInformation> branchInformations)
        {
            this.branchInformations = branchInformations;
        }

        public IEnumerable<BranchInformation> branchInformations { get; private set; }
    }

    [System.Serializable]
    public class BranchOccursEvent : UnityEvent<BranchOccursEventData>
    {
    }


    public class BranchSelectedEventData
    {
        public BranchSelectedEventData(BranchInformation selectedBranchInformation)
        {
            this.selectedBranchInformation = selectedBranchInformation;
        }

        public BranchInformation selectedBranchInformation { get; private set; }
    }

    [System.Serializable]
    public class BranchSelectedEvent : UnityEvent<BranchSelectedEventData>
    {
    }

    public class CurrentRouteEndedEventData
    {
        public CurrentRouteEndedEventData(string endName)
        {
            this.endName = endName;
        }

        public string endName { get; private set; }
    }

    [System.Serializable]
    public class CurrentRouteEndedEvent : UnityEvent<CurrentRouteEndedEventData>
    {
    }

    #endregion

    /// <inheritdoc />
    /// <summary>
    /// This class manages the AVG game state.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public string scriptPath;
        public CheckpointManager checkpointManager;

        private readonly ScriptLoader scriptLoader = new ScriptLoader();
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

        #region events

        /// <summary>
        /// This event will be triggered if whe content of the dialogue will change. It will be triggered before
        /// the lazy execution block of the next dialogue is invoked.
        /// </summary>
        public UnityEvent DialogueWillChange;

        /// <summary>
        /// This event will be triggered if the content of the dialogue has changed. New dialogue text will be
        /// sent to all listeners
        /// </summary>
        public DialogueChangedEvent DialogueChanged;

        /// <summary>
        /// This event will be triggered if the node has changed. The name and discription of the new node will be
        /// sent to all listeners
        /// </summary>
        public NodeChangedEvent NodeChanged;

        /// <summary>
        /// This event will be triggered if branches occur. The player has to choose which branch to take
        /// </summary>
        public BranchOccursEvent BranchOccurs;

        /// <summary>
        /// This event will be triggered if a branch is selected
        /// </summary>
        public BranchSelectedEvent BranchSelected;

        /// <summary>
        /// This event will be triggered if the story reaches an end
        /// </summary>
        public CurrentRouteEndedEvent CurrentRouteEnded;

        #endregion

        private readonly List<string> voicesOfNextDialogue = new List<string>();

        /// <summary>
        /// Add a voice clip to be played on the next dialogue
        /// </summary>
        /// <remarks>
        /// This method should be called by Character controllers
        /// </remarks>
        /// <param name="audioClipName">Name of the voice clip</param>
        public void AddVoiceClipOfNextDialogue(string audioClipName)
        {
            voicesOfNextDialogue.Add(audioClipName);
        }

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

            if (checkpointManager.IsReached(currentNode.name, currentIndex) == null)
            {
                // tell the checkpoint manager a new dialogue entry has been reached
                checkpointManager.SetReached(currentNode.name, currentIndex, GetGameStateStepRestoreEntry());
            }

            DialogueWillChange.Invoke();
            currentDialogueEntry.ExecuteAction();
            DialogueChanged.Invoke(
                new DialogueChangedEventData(currentNode.name, currentIndex, currentDialogueEntry.text,
                    new List<string>(voicesOfNextDialogue)));
            voicesOfNextDialogue.Clear();
        }

        /// <summary>
        /// Move on to the next node
        /// </summary>
        /// <param name="nextNode">The next node to move to</param>
        private void MoveToNextNode(FlowChartNode nextNode)
        {
            walkedThroughNodes.Add(nextNode.name);
            currentNode = nextNode;
            currentIndex = 0;
            NodeChanged.Invoke(new NodeChangedEventData(currentNode.name, currentNode.description));
            UpdateGameState();
        }

        /// <summary>
        /// Move back to a previously stepped node
        /// </summary>
        /// <param name="nodeName">the node to move to</param>
        /// <param name="dialogueIndex">the index of the dialogue to move to</param>
        public void MoveBackTo(string nodeName, int dialogueIndex)
        {
            // restore history
            var backNodeIndex = walkedThroughNodes.FindLastIndex(x => x == nodeName);
            if (backNodeIndex < 0)
            {
                throw new ArgumentException(string.Format("Nova: node {0} has not been walked through", nodeName));
            }

            var nodeHistoryRemoveLength = walkedThroughNodes.Count - backNodeIndex - 1;
            walkedThroughNodes.RemoveRange(backNodeIndex + 1, nodeHistoryRemoveLength);

            // its impossible to branch when goes backward
            isBranching = false;

            // update current node
            currentIndex = dialogueIndex;
            if (nodeName != currentNode.name)
            {
                currentNode = flowChartTree.FindNode(nodeName);
                NodeChanged.Invoke(new NodeChangedEventData(currentNode.name, currentNode.description));
            }

            // restore status
            Restore(checkpointManager.IsReached(currentNode.name, currentIndex));

            // Update game state
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
            MoveToNextNode(startNode);
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
            Assert.IsNotNull(currentNode, "Call Step before the game start");

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
                    MoveToNextNode(currentNode.Next);
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
                    BranchOccurs.Invoke(new BranchOccursEventData(currentNode.GetAllBranches()));
                    break;
                case FlowChartNodeType.End:
                    var endName = flowChartTree.GetEndName(currentNode);
                    if (!checkpointManager.IsReached(endName))
                    {
                        // mark the end as reached
                        checkpointManager.SetReached(endName);
                    }

                    CurrentRouteEnded.Invoke(new CurrentRouteEndedEventData(endName));
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
            var selectedBranchInfo = currentNode.GetAllBranches().First(x => x.name == branchName);
            var nextNode = currentNode.GetNext(branchName);
            if (!checkpointManager.IsReached(currentNode.name, branchName))
            {
                // tell the checkpoint manager a branch has been selected
                checkpointManager.SetReached(currentNode.name, branchName);
            }

            BranchSelected.Invoke(new BranchSelectedEventData(selectedBranchInfo));
            MoveToNextNode(nextNode);
        }

        /// <summary>
        /// All restorable objects
        /// </summary>
        private readonly Dictionary<string, IRestorable> restorables = new Dictionary<string, IRestorable>();

        /// <summary>
        /// Register a new restorable object to the game state
        /// </summary>
        /// <param name="restorable">The restorable to be added</param>
        /// <exception cref="ArgumentException">
        /// Throwed when the name of the restorable object is duplicated defined or the name of the restorable object
        /// is null
        /// </exception>
        public void AddRestorable(IRestorable restorable)
        {
            try
            {
                restorables.Add(restorable.restorableObjectName, restorable);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("Nova: a restorable should have an unique and not null name", ex);
            }
        }

        /// <summary>
        /// Get all restore data from registered restorables
        /// </summary>
        /// <returns>a game state step restore entry that contains all restore datas for the current dialogue</returns>
        private GameStateStepRestoreEntry GetGameStateStepRestoreEntry()
        {
            var restoreDatas = new Dictionary<string, IRestoreData>();
            foreach (var restorable in restorables)
            {
                restoreDatas[restorable.Key] = restorable.Value.GetRestoreData();
            }

            return new GameStateStepRestoreEntry(restoreDatas);
        }

        /// <summary>
        /// Restore all restorables
        /// </summary>
        /// <param name="restoreDatas">restore datas</param>
        private void Restore(GameStateStepRestoreEntry restoreDatas)
        {
            foreach (var restorable in restorables)
            {
                restorable.Value.Restore(restoreDatas[restorable.Key]);
            }
        }
    }
}