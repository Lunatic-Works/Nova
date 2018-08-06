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
        public DialogueChangedEventData(string nodeName, int dialogueIndex, string text,
            IEnumerable<string> voicesForNextDialogue)
        {
            this.nodeName = nodeName;
            this.dialogueIndex = dialogueIndex;
            this.text = text;
            this.voicesForNextDialogue = voicesForNextDialogue;
        }

        public string nodeName { get; private set; }
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

    public class BookmarkWillLoadData
    {
    }

    [System.Serializable]
    public class BookmarkWillLoadEvent : UnityEvent<BookmarkWillLoadData>
    {
    }

    #endregion

    /// <inheritdoc />
    /// <summary>
    /// This class manages the AVG game state.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        [SerializeField] private string scriptPath;
        private CheckpointManager checkpointManager;

        private readonly ScriptLoader scriptLoader = new ScriptLoader();
        private FlowChartTree flowChartTree;

        private void Awake()
        {
            scriptLoader.Init(scriptPath);
            flowChartTree = scriptLoader.GetFlowChartTree();
            checkpointManager = GetComponent<CheckpointManager>();
        }

        private static GameState _instance = null;

        /// <summary>
        /// Get the game state instance attached under current game controller object
        /// </summary>
        public static GameState Instance
        {
            get
            {
                if (_instance == null)
                {
                    var gameController = Utils.FindGameController();
                    _instance = gameController.GetComponent<GameState>();
                    if (_instance == null)
                    {
                        Debug.Log("Nova: Attaching game state to game controller.");
                        _instance = gameController.AddComponent<GameState>();
                        _instance.scriptPath = "";
                        Debug.LogWarning("Nova: Default script path is empty string.");
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Reset instance when old instance is destroyed
        /// </summary>
        private void OnDestroy()
        {
            _instance = null;
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
        /// This should only be modified by UpdateGameState
        /// </summary>
        private int oldIndex = -1;

        /// <summary>
        /// The current dialogueEntry
        /// </summary>
        private DialogueEntry currentDialogueEntry;

        /// <summary>
        /// BranchOccurs has been triggered, but no branch has been selected
        /// </summary>
        private bool isBranching = false;

        /// <summary>
        /// CurrentRouteEnded has been triggered
        /// </summary>
        private bool ended = false;

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

        /// <summary>
        /// A book mark will be loaded
        /// </summary>
        public BookmarkWillLoadEvent BookmarkWillLoad;

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
        /// The game state will be updated according to walkedThroughNodes and current dialogue index.
        /// This method will check if the game state has changed and trigger proper events
        /// </remarks>
        /// <param name="forceRefreshDialogue">refresh dialogue no matter the game state has change or not</param>
        /// <param name="forceRefreshNode">refresh the node no matter the node has changed or not</param>
        private void UpdateGameState(bool forceRefreshDialogue = false, bool forceRefreshNode = false)
        {
            Assert.IsFalse(walkedThroughNodes.Count == 0,
                "Nova: walkedThroughNodes is empty, can not update game state.");

            // update current node
            var desiredNodeName = walkedThroughNodes.Last();
            var nodeChanged = currentNode == null || currentNode.name != desiredNodeName;

            if (nodeChanged || forceRefreshNode)
            {
                currentNode = flowChartTree.FindNode(desiredNodeName);
                NodeChanged.Invoke(new NodeChangedEventData(currentNode.name, currentNode.description));
            }

            // update dialogue
            var dialogueChanged = nodeChanged || currentIndex != oldIndex;

            if (dialogueChanged || forceRefreshDialogue)
            {
                Assert.IsTrue(currentIndex >= 0 && currentIndex < currentNode.DialogueEntryCount,
                    "Nova: dialogue index out of range");
                currentDialogueEntry = currentNode.GetDialogueEntryAt(currentIndex);
                oldIndex = currentIndex;

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
        }

        /// <summary>
        /// Move on to the next node
        /// </summary>
        /// <param name="nextNode">The next node to move to</param>
        private void MoveToNextNode(FlowChartNode nextNode)
        {
            Assert.IsFalse(walkedThroughNodes.Count != 0 && nextNode.name == walkedThroughNodes.Last());
            walkedThroughNodes.Add(nextNode.name);
            currentIndex = 0;
            UpdateGameState();
        }

        /// <summary>
        /// Move back to a previously stepped node
        /// </summary>
        /// <param name="nodeName">the node to move to</param>
        /// <param name="dialogueIndex">the index of the dialogue to move to</param>
        public void MoveBackTo(string nodeName, int dialogueIndex,
            bool forceRefreshDialogue = false, bool forceRefreshNode = false)
        {
            // restore history
            var backNodeIndex = walkedThroughNodes.FindLastIndex(x => x == nodeName);
            Assert.IsFalse(backNodeIndex < 0,
                string.Format("Nova: move back to node {0} that has not been walked through", nodeName));

            // its impossible to branch when goes backward
            isBranching = false;

            // its impossible to be ended when goes backward
            ended = false;

            var nodeHistoryRemoveLength = walkedThroughNodes.Count - backNodeIndex - 1;
            walkedThroughNodes.RemoveRange(backNodeIndex + 1, nodeHistoryRemoveLength);
            currentIndex = dialogueIndex;

            // restore status
            Restore(checkpointManager.IsReached(nodeName, dialogueIndex));

            // Update game state
            UpdateGameState(forceRefreshDialogue, forceRefreshNode);
        }

        /// <summary>
        /// Start the game from the given node
        /// </summary>
        /// <param name="startNode">The node from where the game starts</param>
        private void GameStart(FlowChartNode startNode)
        {
            // clear possible history
            walkedThroughNodes = new List<string>();
            isBranching = false;
            ended = false;
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

        /// <summary>
        /// Check if current state can step forward directly. i.e. something will happen when call Step
        /// </summary>
        public bool canStepForward
        {
            get
            {
                if (currentNode == null)
                {
                    Debug.Log("Nova: Can not call Step before game start.");
                    return false;
                }

                // can step forward when the player is at the middle of a node
                if (currentIndex + 1 < currentNode.DialogueEntryCount)
                {
                    return true;
                }

                switch (currentNode.type)
                {
                    case FlowChartNodeType.Normal:
                        return true;
                    case FlowChartNodeType.Branching:
                        return !isBranching;
                    case FlowChartNodeType.End:
                        return !ended;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Step to the next dialogue entry
        /// </summary>
        /// <returns>true if successfully stepped to the next dialogue or trigger some events</returns>
        public bool Step()
        {
            Assert.IsNotNull(currentNode, "Call Step before the game start");

            // if have a next dialogue entry in the current node, directly step to the next
            if (currentIndex + 1 < currentNode.DialogueEntryCount)
            {
                currentIndex += 1;
                UpdateGameState();
                return true;
            }

            // Reach the end of a node, do something regards on the flow chart node type
            switch (currentNode.type)
            {
                case FlowChartNodeType.Normal:
                    // for normal node, just step directly to the next node
                    MoveToNextNode(currentNode.Next);
                    return true;
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
                    if (ended)
                    {
                        // game end, avoid duplicated calls to CurrentRouteEnded
                        break;
                    }

                    ended = true;
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

            return false;
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
            Assert.IsNotNull(restoreDatas);
            foreach (var restorable in restorables)
            {
                try
                {
                    var restoreData = restoreDatas[restorable.Key];
                    restorable.Value.Restore(restoreData);
                }
                catch (KeyNotFoundException ex)
                {
                    Debug.LogError(
                        string.Format("Key {0} not found in restorableDatas, check if restorable names of " +
                                      "Restorables has changed. If that is true, try clear all checkpoint " +
                                      "files, or undo the change of the restorable name", restorable.Key));
                }
            }
        }

        /// <summary>
        /// Get the Bookmark of current state
        /// </summary>
        public Bookmark GetBookmark()
        {
            return new Bookmark(walkedThroughNodes, currentIndex, currentDialogueEntry.text);
        }

        /// <summary>
        /// Load a Bookmark, restore to the saved state
        /// </summary>
        public void LoadBookmark(Bookmark bookmark)
        {
            BookmarkWillLoad.Invoke(new BookmarkWillLoadData());
            walkedThroughNodes = bookmark.NodeHistory;
            Assert.IsFalse(walkedThroughNodes.Count == 0);
            MoveBackTo(walkedThroughNodes.Last(), bookmark.DialogueIndex, true, true);
        }
    }
}