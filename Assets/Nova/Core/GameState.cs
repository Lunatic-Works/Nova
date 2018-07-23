using System;
using System.Collections.Generic;
using System.Linq;
using Nova.Exceptions;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    public class DialogueChangedEventData
    {
        public DialogueChangedEventData(string labelName, int dialogueIndex, string text,
            IEnumerable<AudioClip> voicesForNextDialogue)
        {
            this.labelName = labelName;
            this.dialogueIndex = dialogueIndex;
            this.text = text;
            this.voicesForNextDialogue = voicesForNextDialogue;
        }

        public string labelName { get; private set; }
        public int dialogueIndex { get; private set; }
        public string text { get; private set; }

        public IEnumerable<AudioClip> voicesForNextDialogue { get; private set; }
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

    /// <inheritdoc />
    /// <summary>
    /// This class manages the AVG game state.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public string scriptPath;

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

        private readonly List<AudioClip> voicesOfNextDialogue = new List<AudioClip>();

        /// <summary>
        /// Add a voice clip for the next dialogue
        /// </summary>
        /// <remarks>
        /// This method should be called by Character controllers
        /// </remarks>
        /// <param name="audioClip"></param>
        public void AddVoiceClipOfNextDialogue(AudioClip audioClip)
        {
            voicesOfNextDialogue.Add(audioClip);
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
            DialogueWillChange.Invoke();
            currentDialogueEntry.ExecuteAction();
            DialogueChanged.Invoke(
                new DialogueChangedEventData(currentNode.name, currentIndex, currentDialogueEntry.text,
                    voicesOfNextDialogue));
            voicesOfNextDialogue.Clear();
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
            NodeChanged.Invoke(new NodeChangedEventData(currentNode.name, currentNode.description));
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
            if (currentNode == null)
            {
                Debug.LogError("Call Step before the game start");
                return;
            }

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
                    BranchOccurs.Invoke(new BranchOccursEventData(currentNode.GetAllBranches()));
                    break;
                case FlowChartNodeType.End:
                    CurrentRouteEnded.Invoke(new CurrentRouteEndedEventData(flowChartTree.GetEndName(currentNode)));
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
            MoveToNode(nextNode);
            BranchSelected.Invoke(
                new BranchSelectedEventData(selectedBranchInfo));
        }
    }
}