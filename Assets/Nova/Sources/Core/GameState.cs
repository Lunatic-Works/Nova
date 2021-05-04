using Nova.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Nova
{
    #region Event types and event datas

    public class DialogueChangedData
    {
        public readonly string nodeName;
        public readonly int dialogueIndex;
        public readonly DialogueDisplayData displayData;
        public readonly Dictionary<string, VoiceEntry> voicesForNextDialogue;
        public readonly bool hasBeenReached;

        public DialogueChangedData(string nodeName, int dialogueIndex, DialogueDisplayData displayData,
            Dictionary<string, VoiceEntry> voicesForNextDialogue, bool hasBeenReached)
        {
            this.nodeName = nodeName;
            this.dialogueIndex = dialogueIndex;
            this.displayData = displayData;
            this.voicesForNextDialogue = voicesForNextDialogue;
            this.hasBeenReached = hasBeenReached;
        }
    }

    public class NodeChangedData
    {
        public readonly string nodeName;

        public NodeChangedData(string nodeName)
        {
            this.nodeName = nodeName;
        }
    }

    public class BranchOccursData
    {
        public readonly IEnumerable<BranchInformation> branchInformations;

        public BranchOccursData(IEnumerable<BranchInformation> branchInformations)
        {
            this.branchInformations = branchInformations;
        }
    }

    public class BranchSelectedData
    {
        public readonly BranchInformation selectedBranchInformation;

        public BranchSelectedData(BranchInformation selectedBranchInformation)
        {
            this.selectedBranchInformation = selectedBranchInformation;
        }
    }

    public class CurrentRouteEndedData
    {
        public readonly string endName;

        public CurrentRouteEndedData(string endName)
        {
            this.endName = endName;
        }
    }

    public class BookmarkWillLoadData { }

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
            try
            {
                scriptLoader.Init(scriptPath);
                flowChartTree = scriptLoader.GetFlowChartTree();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                Utils.Exit();
            }

            checkpointManager = GetComponent<CheckpointManager>();
            LuaRuntime.Instance.BindObject("variables", variables);
            LuaRuntime.Instance.BindObject("advancedDialogueHelper", advancedDialogueHelper);
        }

        /// <summary>
        /// Used for faster iterations during the development, like to preview changes in scripts without restarting the game
        /// Assume we already moved to the first dialogue entry in the current node, and provided initial variables hash
        /// </summary>
        public void ReloadScripts()
        {
            LuaRuntime.Instance.InitRequires();
            scriptLoader.ForceInit(scriptPath);
            flowChartTree = scriptLoader.GetFlowChartTree();
            UpdateGameState(true, true, false, false);
        }

        #region States

        /// <summary>
        /// Nodes that has been walked through
        /// </summary>
        private List<string> walkedThroughNodes;

        /// <summary>
        /// The current flow chart node
        /// </summary>
        public FlowChartNode currentNode { get; private set; }

        /// <summary>
        /// The index of the current dialogue entry in the current node
        /// </summary>
        public int currentIndex { get; private set; }

        /// <summary>
        /// The current dialogueEntry
        /// </summary>
        /// <remarks>
        /// Only set by ResetGameState() and UpdateGameState()
        /// </remarks>
        private DialogueEntry currentDialogueEntry;

        /// <summary>
        /// Current state of variables
        /// </summary>
        public readonly Variables variables = new Variables();

        private string lastCheckpointVariablesHash;
        private string lastVariablesHashBeforeAction;

        private enum State
        {
            Normal,
            IsBranching,
            Ended,
            ActionRunning
        }

        private State state = State.Normal;

        /// <summary>
        /// BranchOccurs has been triggered, but no branch has been selected
        /// </summary>
        private bool isBranching => state == State.IsBranching;

        /// <summary>
        /// CurrentRouteEnded has been triggered
        /// </summary>
        private bool ended => state == State.Ended;

        /// <summary>
        /// True when action is running
        /// </summary>
        private bool actionIsRunning => state == State.ActionRunning;

        #endregion

        /// <summary>
        /// Reset GameState, make it the same as the game not start
        /// </summary>
        /// <remarks>
        /// No event will be triggered when this method is called
        /// </remarks>
        public void ResetGameState()
        {
            if (CheckActionRunning()) return;

            // Reset all
            walkedThroughNodes = null;
            currentNode = null;
            currentIndex = 0;
            currentDialogueEntry = null;
            variables.Reset();
            state = State.Normal;

            // Restore scene
            if (checkpointManager.clearSceneRestoreEntry != null)
            {
                RestoreRaw(checkpointManager.clearSceneRestoreEntry);
            }
        }

        #region Events

        /// <summary>
        /// This event will be triggered if whe content of the dialogue will change. It will be triggered before
        /// the lazy execution block of the next dialogue is invoked.
        /// </summary>
        public event UnityAction DialogueWillChange;

        /// <summary>
        /// This event will be triggered if the content of the dialogue has changed. New dialogue text will be
        /// sent to all listeners
        /// </summary>
        public event UnityAction<DialogueChangedData> DialogueChanged;

        /// <summary>
        /// This event will be triggered if the node has changed. The name and description of the new node will be
        /// sent to all listeners
        /// </summary>
        public event UnityAction<NodeChangedData> NodeChanged;

        /// <summary>
        /// This event will be triggered if branches occur. The player has to choose which branch to take
        /// </summary>
        public event UnityAction<BranchOccursData> BranchOccurs;

        /// <summary>
        /// This event will be triggered if a branch is selected
        /// </summary>
        public event UnityAction<BranchSelectedData> BranchSelected;

        /// <summary>
        /// This event will be triggered if the story reaches an end
        /// </summary>
        public event UnityAction<CurrentRouteEndedData> CurrentRouteEnded;

        /// <summary>
        /// A book mark will be loaded
        /// </summary>
        public event UnityAction<BookmarkWillLoadData> BookmarkWillLoad;

        #endregion

        /// <summary>
        /// Check if action is running
        /// </summary>
        /// <returns>true if action is running</returns>
        private bool CheckActionRunning()
        {
            Assert.IsFalse(actionIsRunning,
                "Nova: Cannot change game flow when the GameState is paused by methods.");
            return actionIsRunning;
        }

        #region Action pause lock

        /// <summary>
        /// Acquiring this lock will make the game state pause and wait for the lock being released
        /// </summary>
        private readonly CounterLock actionPauseLock = new CounterLock();

        /// <summary>
        /// methods invoked from lazy execution blocks can ask the GameState to pause, i.e. dialogue changes only after
        /// these method release the GameState
        /// </summary>
        public void ActionAcquirePause()
        {
            this.RuntimeAssert(actionIsRunning, "Action pause lock can only be acquired when action is running.");
            actionPauseLock.Acquire();
            // all action that potentially runs for a long time should not be laid between checkpoints
            EnsureCheckpointOnNextDialogue();
        }

        /// <summary>
        /// methods that acquire pause of the GameState should release the GameState when their actions finishes
        /// </summary>
        public void ActionReleasePause()
        {
            this.RuntimeAssert(actionIsRunning, "Action pause lock can only be released when action is running.");
            actionPauseLock.Release();
        }

        #endregion

        #region Voice

        private readonly Dictionary<string, VoiceEntry> voicesOfNextDialogue = new Dictionary<string, VoiceEntry>();

        /// <summary>
        /// Add a voice clip to be played on the next dialogue
        /// </summary>
        /// <remarks>
        /// This method should be called by Character controllers
        /// </remarks>
        /// <param name="characterName">the unique id of character, usually its lua variable name</param>
        /// <param name="voiceEntry"></param>
        public void AddVoiceClipOfNextDialogue(string characterName, VoiceEntry voiceEntry)
        {
            voicesOfNextDialogue.Add(characterName, voiceEntry);
        }

        #endregion

        /// <summary>
        /// Called after the current node or the index of the current dialogue entry has changed.
        /// </summary>
        /// <remarks>
        /// Trigger events according to the current game state and how it is changed
        /// </remarks>
        /// <param name="nodeChanged"></param>
        /// <param name="dialogueChanged"></param>
        /// <param name="firstEntryOfNode"></param>
        /// <param name="dialogueStepped"></param>
        private void UpdateGameState(bool nodeChanged, bool dialogueChanged, bool firstEntryOfNode,
            bool dialogueStepped)
        {
            // Debug.Log($"UpdateGameState begin {currentNode.name} {currentIndex} {stepNumFromLastCheckpoint} {restrainCheckpoint} {forceCheckpoint}");

            if (nodeChanged)
            {
                // Debug.Log($"Nova: node changed to {currentNode.name}");

                if (firstEntryOfNode) EnsureCheckpoint(); // always get a checkpoint at the beginning of the node

                NodeChanged?.Invoke(new NodeChangedData(currentNode.name));
            }

            if (dialogueChanged)
            {
                this.RuntimeAssert(currentIndex >= 0 && currentIndex < currentNode.dialogueEntryCount,
                    "Dialogue index out of range.");

                if (!firstEntryOfNode && dialogueStepped)
                {
                    stepNumFromLastCheckpoint++;
                }

                var gameStateRestoreEntry = checkpointManager.IsReached(currentNode.name, currentIndex, variables.hash);
                if (gameStateRestoreEntry == null)
                {
                    // Tell the checkpoint manager a new dialogue entry has been reached
                    // Debug.Log($"UpdateGameState SetReached {currentNode.name} {currentIndex} {variables.hash}");
                    checkpointManager.SetReached(currentNode.name, currentIndex, variables,
                        GetGameStateStepRestoreEntry());
                }

                // Change states after creating or restoring from checkpoint
                if (shouldSaveRealCheckpoint)
                {
                    stepNumFromLastCheckpoint = 0;
                }

                if (gameStateRestoreEntry != null)
                {
                    this.RuntimeAssert(stepNumFromLastCheckpoint == gameStateRestoreEntry.stepNumFromLastCheckpoint,
                        $"StepNumFromLastCheckpoint mismatch: {stepNumFromLastCheckpoint} {gameStateRestoreEntry.stepNumFromLastCheckpoint}");
                    this.RuntimeAssert(restrainCheckpoint == gameStateRestoreEntry.restrainCheckpointNum,
                        $"RestrainCheckpointNum mismatch: {restrainCheckpoint} {gameStateRestoreEntry.restrainCheckpointNum}");
                }

                if (checkpointRestrained)
                {
                    restrainCheckpoint--;
                    if (restrainCheckpoint == 1)
                    {
                        Debug.LogWarning($"Nova: restrainCheckpoint reaches 1");
                    }
                }

                // As the action in this dialogue entry will rerun, it's fine to just reset forceCheckpoint to false
                forceCheckpoint = false;

                DialogueWillChange?.Invoke();

                state = State.ActionRunning;
                lastVariablesHashBeforeAction = variables.hash;
                currentDialogueEntry = currentNode.GetDialogueEntryAt(currentIndex);
                currentDialogueEntry.ExecuteAction();
                StartCoroutine(WaitActionEnd(gameStateRestoreEntry != null));
            }

            // Debug.Log($"UpdateGameState end {currentNode.name} {currentIndex} {stepNumFromLastCheckpoint} {restrainCheckpoint} {forceCheckpoint} {currentDialogueEntry?.displayData.FormatNameDialogue()}");
        }

        private readonly AdvancedDialogueHelper advancedDialogueHelper = new AdvancedDialogueHelper();

        private IEnumerator WaitActionEnd(bool hasBeenReached)
        {
            while (actionPauseLock.isLocked)
            {
                yield return null;
            }

            state = State.Normal;

            // everything that makes game state pause has ended, change dialogue
            // TODO: use advancedDialogueHelper to override dialogue
            // The game author should define overriding dialogues for each locale
            // By the way, we don't need to store all dialogues in save data,
            // just those overriden
            DialogueChanged?.Invoke(new DialogueChangedData(currentNode.name, currentIndex,
                currentDialogueEntry.displayData, new Dictionary<string, VoiceEntry>(voicesOfNextDialogue),
                hasBeenReached));

            voicesOfNextDialogue.Clear();

            var pendingJumpTarget = advancedDialogueHelper.GetJump();
            if (pendingJumpTarget != null)
            {
                var node = flowChartTree.GetNode(pendingJumpTarget);
                this.RuntimeAssert(node != null, "Node " + pendingJumpTarget + " does not exist!");
                MoveToNextNode(node);
            }
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
            UpdateGameState(true, true, true, true);
        }

        public bool isMovingBack { get; private set; }

        /// <summary>
        /// Move back to a previously stepped node, the lazy execution block and callbacks at the target dialogue entry
        /// will be executed again.
        /// </summary>
        /// <param name="nodeName">the node to move to</param>
        /// <param name="dialogueIndex">the index of the dialogue to move to</param>
        /// <param name="variablesHash">hash of variables of the target</param>
        /// <param name="clearFuture">clear saved checkpoints in the future</param>
        public void MoveBackTo(string nodeName, int dialogueIndex, string variablesHash, bool clearFuture = false)
        {
            if (CheckActionRunning()) return;

            // animation should stop
            NovaAnimation.StopAll(AnimationType.All ^ AnimationType.UI);

            // restore history
            var backNodeIndex = walkedThroughNodes.FindLastIndex(x => x == nodeName);
            Assert.IsFalse(backNodeIndex < 0,
                $"Nova: move back to node {nodeName} that has not been walked through.");

            // state should be normal when goes backward
            state = State.Normal;

            if (clearFuture)
            {
                // all save data of nodes to be removed are deleted
                for (var i = backNodeIndex + 1; i < walkedThroughNodes.Count; i++)
                {
                    checkpointManager.UnsetReached(walkedThroughNodes[i]);
                }

                // all save data of later dialogues are deleted
                for (var i = dialogueIndex + 1; i < flowChartTree.GetNode(nodeName).dialogueEntryCount; i++)
                {
                    checkpointManager.UnsetReached(nodeName, i);
                }
            }

            var nodeHistoryRemoveLength = walkedThroughNodes.Count - backNodeIndex - 1;
            walkedThroughNodes.RemoveRange(backNodeIndex + 1, nodeHistoryRemoveLength);
            currentNode = flowChartTree.GetNode(walkedThroughNodes.Last());
            currentIndex = dialogueIndex;

            // restore status
            var entry = checkpointManager.IsReached(nodeName, dialogueIndex, variablesHash);
            if (entry == null)
            {
                Debug.LogWarning(
                    $"Nova: Unable to find node with varhash = {variablesHash}, falling back to any variable.");
                entry = checkpointManager.IsReachedForAnyVariables(nodeName, dialogueIndex);
            }

            Restore(entry);
        }

        #region Game start

        public void SaveInitialState()
        {
            // Save a clean state of game scene
            if (checkpointManager.clearSceneRestoreEntry == null)
            {
                checkpointManager.clearSceneRestoreEntry = GetGameStateStepRestoreEntryRaw();
            }
        }

        /// <summary>
        /// Start the game from the given node
        /// </summary>
        /// <param name="startNode">The node from where the game starts</param>
        private void GameStart(FlowChartNode startNode)
        {
            // clear possible history
            walkedThroughNodes = new List<string>();
            state = State.Normal;
            MoveToNextNode(startNode);
        }

        /// <summary>
        /// Start the game from the default start point
        /// </summary>
        public void GameStart()
        {
            if (CheckActionRunning()) return;
            var startNode = flowChartTree.defaultStartNode;
            GameStart(startNode);
        }

        /// <summary>
        /// Start the game from a named start point
        /// </summary>
        /// <param name="startName">the name of the start</param>
        public void GameStart(string startName)
        {
            if (CheckActionRunning()) return;
            var startNode = flowChartTree.GetStartNode(startName);
            GameStart(startNode);
        }

        public string[] GetAllStartNodeNames()
        {
            return flowChartTree.GetAllStartNodeNames();
        }

        public string[] GetAllUnlockedStartNodeNames()
        {
            return flowChartTree.GetAllUnlockedStartNodeNames();
        }

        #endregion

        /// <summary>
        /// Check if current state can step forward directly. i.e. something will happen when call Step
        /// </summary>
        public bool canStepForward
        {
            get
            {
                if (currentNode == null)
                {
                    Debug.LogWarning("Nova: Cannot call Step() before game start.");
                    return false;
                }

                // can step forward only when the state is normal
                return state == State.Normal;
            }
        }

        /// <summary>
        /// Step to the next dialogue entry
        /// </summary>
        /// <returns>true if successfully stepped to the next dialogue or trigger some events</returns>
        public bool Step()
        {
            Assert.IsNotNull(currentNode, "Nova: Calling Step before the game start.");
            if (CheckActionRunning()) return false;
            if (state != State.Normal) return false; // can step forward only when state is normal

            // if have a next dialogue entry in the current node, directly step to the next
            if (currentIndex + 1 < currentNode.dialogueEntryCount)
            {
                currentIndex += 1;
                UpdateGameState(false, true, false, true);
                return true;
            }

            // Reach the end of a node, do something regards on the flow chart node type
            switch (currentNode.type)
            {
                case FlowChartNodeType.Normal:
                    // for normal node, just step directly to the next node
                    MoveToNextNode(currentNode.next);
                    // successfully move to the next node
                    return true;
                case FlowChartNodeType.Branching:
                    // A branch occurs, inform branch event listeners
                    state = State.IsBranching;
                    BranchOccurs?.Invoke(new BranchOccursData(currentNode.GetAllBranches()));

                    break;
                case FlowChartNodeType.End:
                    state = State.Ended;
                    var endName = flowChartTree.GetEndName(currentNode);
                    if (!checkpointManager.IsReached(endName))
                    {
                        // mark the end as reached
                        checkpointManager.SetReached(endName);
                    }

                    CurrentRouteEnded?.Invoke(new CurrentRouteEndedData(endName));

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        /// <summary>
        /// Call this method to choose a branch
        /// </summary>
        /// <remarks>
        /// This method should be called when a branch happens, otherwise An InvalidAccessException will be raised
        /// </remarks>
        /// <param name="branchName">The name of the branch</param>
        /// <exception cref="InvalidAccessException">An InvalidAccessException will be thrown if this method
        /// is not called on branch happens</exception>
        public void SelectBranch(string branchName)
        {
            if (CheckActionRunning()) return;
            if (!isBranching)
            {
                throw new InvalidAccessException("Nova: SelectBranch() should only be called when a branch happens.");
            }

            state = State.Normal;
            var selectedBranchInfo = currentNode.GetAllBranches().First(x => x.name == branchName);
            var nextNode = currentNode.GetNext(branchName);
            if (!checkpointManager.IsReached(currentNode.name, branchName, variables.hash))
            {
                // tell the checkpoint manager a branch has been selected
                checkpointManager.SetReached(currentNode.name, branchName, variables);
            }

            BranchSelected?.Invoke(new BranchSelectedData(selectedBranchInfo));

            MoveToNextNode(nextNode);
        }

        #region Manage restorables

        /// <summary>
        /// All restorable objects
        /// </summary>
        private readonly Dictionary<string, IRestorable> restorables = new Dictionary<string, IRestorable>();

        /// <summary>
        /// Register a new restorable object to the game state
        /// </summary>
        /// <param name="restorable">The restorable to be added</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the name of the restorable object is duplicated defined or the name of the restorable object
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
        /// remove a restorable
        /// </summary>
        /// <param name="restorable">the restorable to be removed</param>
        public void RemoveRestorable(IRestorable restorable)
        {
            restorables.Remove(restorable.restorableObjectName);
        }

        /// <summary>
        /// Not all states can be easily restored, like persistent animations.
        /// Store some checkpoints, and other states can be restored by re-executing from the last checkpoint.
        /// At least one checkpoint will be saved every maxStepNumFromLastCheckpoint, except during persistent animations.
        /// </summary>
        public int maxStepNumFromLastCheckpoint = 10;

        private int stepNumFromLastCheckpoint;

        /// <summary>
        /// Restrain saving checkpoints.
        /// This feature is necessary for restoring persistent animations.
        /// This restraint has higher priority than EnsureCheckpoint().
        /// </summary>
        private int restrainCheckpoint;

        private bool checkpointRestrained => restrainCheckpoint > 0;

        /// <summary>
        /// Restrain saving checkpoints for given steps. Force overwrite the number of restraining steps when authorized is true.
        /// </summary>
        /// <param name="steps">the steps to restrain checkpoints</param>
        /// <param name="authorized">if the new restraining step num should overwrite the old one</param>
        public void RestrainCheckpoint(int steps, bool authorized = false)
        {
            // check overwrite
            if (!authorized && restrainCheckpoint >= steps) return;
            // non-negative
            if (steps < 0) steps = 0;
            restrainCheckpoint = steps;
        }

        /// <summary>
        /// Should be used before any state changes in a dialogue entry
        /// </summary>
        public void EnsureCheckpoint()
        {
            // Debug.Log("EnsureCheckpoint");

            if (checkpointRestrained) return;
            stepNumFromLastCheckpoint = 0;
            var checkpoint = checkpointManager.IsReached(currentNode.name, currentIndex, variables.hash);
            if (!(checkpoint is GameStateStepRestoreCheckpointEntry))
            {
                // Debug.Log($"EnsureCheckpoint SetReached {currentNode.name} {currentIndex} {variables.hash}");
                checkpointManager.SetReached(currentNode.name, currentIndex, variables,
                    GetGameStateStepRestoreEntryRaw());
            }
        }

        /// <summary>
        /// Used to force save a checkpoint before a persistent animation begins.
        /// </summary>
        private bool forceCheckpoint = false;

        public void EnsureCheckpointOnNextDialogue()
        {
            // Debug.Log("EnsureCheckpointOnNextDialogue");

            forceCheckpoint = true;
        }

        private bool shouldSaveRealCheckpoint => forceCheckpoint ||
                                                 (!checkpointRestrained && stepNumFromLastCheckpoint >=
                                                     maxStepNumFromLastCheckpoint);

        /// <summary>
        /// Force to get the current game state as a checkpoint
        /// </summary>
        /// <returns>current game state checkpoint</returns>
        private GameStateStepRestoreCheckpointEntry GetGameStateStepRestoreEntryRaw()
        {
            var restoreDatas = new Dictionary<string, IRestoreData>();
            foreach (var restorable in restorables)
            {
                restoreDatas[restorable.Key] = restorable.Value.GetRestoreData();
            }

            lastCheckpointVariablesHash = variables.hash;
            // Debug.Log($"Nova: Saving checkpoint and setting lastCheckpointVariablesHash = {lastCheckpointVariablesHash}");
            return new GameStateStepRestoreCheckpointEntry(restoreDatas, variables, restrainCheckpoint);
        }

        /// <summary>
        /// Get all restore data from registered restorables
        /// </summary>
        /// <returns>a game state step restore entry that contains all restore datas for the current dialogue</returns>
        private GameStateStepRestoreEntry GetGameStateStepRestoreEntry()
        {
            if (shouldSaveRealCheckpoint)
            {
                return GetGameStateStepRestoreEntryRaw();
            }

            return new GameStateStepRestoreSimpleEntry(stepNumFromLastCheckpoint, restrainCheckpoint,
                lastCheckpointVariablesHash);
        }

        private void RestoreRaw(GameStateStepRestoreCheckpointEntry restoreDatas)
        {
            // Debug.Log($"Nova: Restoring checkpoint from varhash = {restoreDatas.variables.hash}");
            Assert.IsNotNull(restoreDatas);

            stepNumFromLastCheckpoint = 0;
            restrainCheckpoint = restoreDatas.restrainCheckpointNum;
            forceCheckpoint = false;

            variables.CopyFrom(restoreDatas.variables);

            foreach (var restorable in from entry in restorables
                orderby (entry.Value as IPrioritizedRestorable)?.priority ?? RestorablePriority.Normal descending
                select entry)
            {
                try
                {
                    var restoreData = restoreDatas[restorable.Key];
                    restorable.Value.Restore(restoreData);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogErrorFormat("Nova: Key {0} not found in restoreDatas. Check if the restorable name " +
                                         "has changed. If that is true, try clear all checkpoint " +
                                         "files, or undo the change of the restorable name.", restorable.Key);
                }
            }
        }

        /// <summary>
        /// Get the node name and the dialogue index before specified steps
        /// </summary>
        /// <param name="steps">number to step back</param>
        /// <param name="nodeName">node name at given steps before</param>
        /// <param name="dialogueIndex">dialogue index at given steps before</param>
        /// <returns>true when success, false when step is too large or a minus number</returns>
        public bool SeekBackStep(int steps, out string nodeName, out int dialogueIndex)
        {
            if (steps < 0)
            {
                nodeName = "";
                dialogueIndex = -1;
                return false;
            }

            if (currentIndex >= steps)
            {
                nodeName = walkedThroughNodes.Last();
                dialogueIndex = currentIndex - steps;
                return true;
            }

            // The following code won't be frequently executed, since there is always a checkpoint at the
            // start of the node, and the steps stored in GameStateStepRestoreEntry never steps across node
            // boundary.

            steps -= currentIndex;
            for (var i = walkedThroughNodes.Count - 2; i >= 0; i--)
            {
                var node = flowChartTree.GetNode(walkedThroughNodes[i]);
                if (node.dialogueEntryCount >= steps)
                {
                    nodeName = walkedThroughNodes[i];
                    dialogueIndex = node.dialogueEntryCount - steps;
                    return true;
                }

                steps -= node.dialogueEntryCount;
            }

            nodeName = "";
            dialogueIndex = -1;
            return false;
        }

        /// <summary>
        /// Restore all restorables. The lazy execution block in the target entry will be executed again.
        /// </summary>
        /// <param name="restoreDatas">restore datas</param>
        private void Restore(GameStateStepRestoreEntry restoreDatas)
        {
            // Debug.LogFormat("Steps from last ckpt: {0}", restoreDatas.stepNumFromLastCheckpoint);

            if (restoreDatas is GameStateStepRestoreCheckpointEntry checkpointEntry)
            {
                RestoreRaw(checkpointEntry);
                UpdateGameState(true, true, false, false);
            }
            else if (restoreDatas is GameStateStepRestoreSimpleEntry simpleEntry)
            {
                // Debug.Log($"Nova: Restoring simple entry from lastCheckpointVariablesHash = {simpleEntry.lastCheckpointVariablesHash}");

                if (!SeekBackStep(simpleEntry.stepNumFromLastCheckpoint, out string storedNode,
                    out int storedDialogueIndex))
                {
                    Debug.LogErrorFormat("Nova: Failed to seek back, invalid stepNumFromLastCheckpoint: {0}",
                        simpleEntry.stepNumFromLastCheckpoint);
                }

                isMovingBack = true;
                MoveBackTo(storedNode, storedDialogueIndex, simpleEntry.lastCheckpointVariablesHash);
                for (var i = 0; i < simpleEntry.stepNumFromLastCheckpoint; i++)
                {
                    if (i == simpleEntry.stepNumFromLastCheckpoint - 1)
                    {
                        isMovingBack = false;
                    }

                    // Make sure there is no blocking action running
                    NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                    Step();
                }
            }
            else
            {
                throw new ArgumentException($"Nova: {restoreDatas} is not supported.");
            }
        }

        #endregion

        #region Bookmark

        /// <summary>
        /// Get the Bookmark of current state
        /// </summary>
        public Bookmark GetBookmark()
        {
            return new Bookmark(walkedThroughNodes, currentIndex, I18n.__(currentDialogueEntry.dialogues),
                lastVariablesHashBeforeAction);
        }

        /// <summary>
        /// Load a Bookmark, restore to the saved state
        /// </summary>
        public void LoadBookmark(Bookmark bookmark)
        {
            if (CheckActionRunning()) return;

            BookmarkWillLoad?.Invoke(new BookmarkWillLoadData());

            walkedThroughNodes = bookmark.nodeHistory;
            Assert.IsFalse(walkedThroughNodes.Count == 0);
            MoveBackTo(walkedThroughNodes.Last(), bookmark.dialogueIndex, bookmark.variablesHash);
        }

        #endregion
    }
}