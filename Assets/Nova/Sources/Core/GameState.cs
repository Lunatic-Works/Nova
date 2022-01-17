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
    using NodeHistory = CountedHashableList<string>;
    using NodeHistoryEntry = KeyValuePair<string, int>;

    #region Event types and datas

    public class DialogueWillChangeData { }

    [Serializable]
    public class DialogueWillChangeEvent : UnityEvent<DialogueWillChangeData> { }

    public class DialogueChangedData
    {
        public readonly NodeHistoryEntry nodeHistoryEntry;
        public readonly int dialogueIndex;
        public readonly DialogueDisplayData displayData;
        public readonly Dictionary<string, VoiceEntry> voicesForNextDialogue;
        public readonly bool hasBeenReached;
        public readonly bool hasBeenReachedWithAnyHistory;

        public DialogueChangedData(NodeHistoryEntry nodeHistoryEntry, int dialogueIndex,
            DialogueDisplayData displayData, Dictionary<string, VoiceEntry> voicesForNextDialogue, bool hasBeenReached,
            bool hasBeenReachedWithAnyHistory)
        {
            this.nodeHistoryEntry = nodeHistoryEntry;
            this.dialogueIndex = dialogueIndex;
            this.displayData = displayData;
            this.voicesForNextDialogue = voicesForNextDialogue;
            this.hasBeenReached = hasBeenReached;
            this.hasBeenReachedWithAnyHistory = hasBeenReachedWithAnyHistory;
        }
    }

    [Serializable]
    public class DialogueChangedEvent : UnityEvent<DialogueChangedData> { }

    public class NodeChangedData
    {
        public readonly string nodeName;

        public NodeChangedData(string nodeName)
        {
            this.nodeName = nodeName;
        }
    }

    [Serializable]
    public class NodeChangedEvent : UnityEvent<NodeChangedData> { }

    public class SelectionOccursData
    {
        [ExportCustomType]
        public class Selection
        {
            public readonly Dictionary<SystemLanguage, string> texts;
            public readonly BranchImageInformation imageInfo;
            public readonly bool active;

            public Selection(Dictionary<SystemLanguage, string> texts, BranchImageInformation imageInfo, bool active)
            {
                this.texts = texts;
                this.imageInfo = imageInfo;
                this.active = active;
            }

            public Selection(string text, BranchImageInformation imageInfo, bool active)
                : this(new Dictionary<SystemLanguage, string>
                {
                    [I18n.DefaultLocale] = text
                }, imageInfo, active) { }
        }

        public readonly List<Selection> selections;

        public SelectionOccursData(List<Selection> selections)
        {
            this.selections = selections;
        }
    }

    [Serializable]
    public class SelectionOccursEvent : UnityEvent<SelectionOccursData> { }

    public class CurrentRouteEndedData
    {
        public readonly string endName;

        public CurrentRouteEndedData(string endName)
        {
            this.endName = endName;
        }
    }

    [Serializable]
    public class CurrentRouteEndedEvent : UnityEvent<CurrentRouteEndedData> { }

    #endregion

    /// <inheritdoc />
    /// <summary>
    /// This class manages the AVG game state.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        [SerializeField] private string scriptPath;

        private CheckpointManager checkpointManager;
        private GameStateCheckpoint clearSceneRestoreEntry;
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
                Utils.Quit();
            }

            checkpointManager = GetComponent<CheckpointManager>();
            LuaRuntime.Instance.BindObject("variables", variables);
            advancedDialogueHelper = new AdvancedDialogueHelper(this);
            LuaRuntime.Instance.BindObject("advancedDialogueHelper", advancedDialogueHelper);
            coroutineHelper = new CoroutineHelper(this);
            LuaRuntime.Instance.BindObject("coroutineHelper", coroutineHelper);
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
        }

        #region States

        /// <summary>
        /// Names of the nodes that have been walked through, including the current node
        /// </summary>
        /// <remarks>
        /// Modified by MoveToNextNode() and MoveBackTo()
        /// </remarks>
        public readonly NodeHistory nodeHistory = new NodeHistory();

        /// <summary>
        /// The current flow chart node
        /// </summary>
        /// <remarks>
        /// Modified by MoveToNextNode() and MoveBackTo()
        /// </remarks>
        public FlowChartNode currentNode { get; private set; }

        /// <summary>
        /// The index of the current dialogue entry in the current node
        /// </summary>
        /// <remarks>
        /// Modified by MoveToNextNode(), MoveBackTo() and Step()
        /// </remarks>
        public int currentIndex { get; private set; }

        /// <summary>
        /// The current dialogueEntry
        /// </summary>
        /// <remarks>
        /// Modified by UpdateGameState()
        /// </remarks>
        private DialogueEntry currentDialogueEntry;

        /// <summary>
        /// The current state of variables
        /// </summary>
        public readonly Variables variables = new Variables();

        private enum State
        {
            Normal,
            Ended,
            ActionRunning
        }

        private State state = State.Normal;

        /// <summary>
        /// currentRouteEnded has been triggered
        /// </summary>
        private bool ended => state == State.Ended;

        /// <summary>
        /// True when any action is running
        /// </summary>
        private bool actionIsRunning => state == State.ActionRunning;

        /// <summary>
        /// Reset GameState, make it the same as the game is not started
        /// </summary>
        /// <remarks>
        /// No event will be triggered when this method is called
        /// </remarks>
        public void ResetGameState()
        {
            CancelAction();

            // Reset all states
            nodeHistory.Clear();
            currentNode = null;
            currentIndex = 0;
            currentDialogueEntry = null;
            variables.Reset();
            state = State.Normal;

            // Restore scene
            if (clearSceneRestoreEntry != null)
            {
                RestoreCheckpoint(clearSceneRestoreEntry);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// This event will be triggered if the content of the dialogue will change. It will be triggered before
        /// the lazy execution block of the next dialogue is invoked.
        /// </summary>
        public DialogueWillChangeEvent dialogueWillChange;

        /// <summary>
        /// This event will be triggered if the content of the dialogue has changed. The new dialogue text will be
        /// sent to all listeners.
        /// </summary>
        public DialogueChangedEvent dialogueChanged;

        /// <summary>
        /// This event will be triggered if the node has changed. The name and the description of the new node will be
        /// sent to all listeners.
        /// </summary>
        public NodeChangedEvent nodeChanged;

        /// <summary>
        /// This event will be triggered if selection occurs, either when branches occur or when a selection is triggered from scripts.
        /// </summary>
        public SelectionOccursEvent selectionOccurs;

        /// <summary>
        /// This event will be triggered if the story reaches an end
        /// </summary>
        public CurrentRouteEndedEvent currentRouteEnded;

        #endregion

        #region Action pause lock

        /// <summary>
        /// Acquiring this lock will make the game state pause and wait for the lock being released
        /// </summary>
        private readonly CounterLock actionPauseLock = new CounterLock();

        /// <summary>
        /// Methods invoked from lazy execution blocks can ask the GameState to pause, i.e. dialogue changes only after
        /// these method release the GameState
        /// </summary>
        public void ActionAcquirePause()
        {
            this.RuntimeAssert(actionIsRunning, "Action pause lock can only be acquired when action is running.");
            actionPauseLock.Acquire();
        }

        /// <summary>
        /// Methods that acquire pause of the GameState should release the GameState when their actions finishes
        /// </summary>
        public void ActionReleasePause()
        {
            this.RuntimeAssert(actionIsRunning, "Action pause lock can only be released when action is running.");
            actionPauseLock.Release();
        }

        public void SignalFence(object value)
        {
            coroutineHelper.fence = value;
        }

        #endregion

        #region Voice

        private readonly Dictionary<string, VoiceEntry> voicesOfNextDialogue = new Dictionary<string, VoiceEntry>();

        /// <summary>
        /// Add a voice clip to be played on the next dialogue
        /// </summary>
        /// <remarks>
        /// This method should be called by CharacterController
        /// </remarks>
        /// <param name="characterName">The unique id of character, usually its Lua variable name</param>
        /// <param name="voiceEntry"></param>
        public void AddVoiceClipOfNextDialogue(string characterName, VoiceEntry voiceEntry)
        {
            voicesOfNextDialogue.Add(characterName, voiceEntry);
        }

        #endregion

        private Coroutine actionCoroutine;

        private void CancelAction()
        {
            if (!actionIsRunning)
            {
                actionCoroutine = null;
                return;
            }

            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
            DialogueEntry.StopActionCoroutine();

            ResetActionContext();
            if (state == State.ActionRunning)
            {
                state = State.Normal;
            }
        }

        private void ResetActionContext()
        {
            voicesOfNextDialogue.Clear();
            advancedDialogueHelper.Reset();
            actionPauseLock.Reset();
            coroutineHelper.Reset();
        }

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
        /// <param name="isCheckpoint"></param>
        /// <param name="onFinish">Callback on finish</param>
        private void UpdateGameState(bool nodeChanged, bool dialogueChanged, bool firstEntryOfNode,
            bool dialogueStepped, bool isCheckpoint, Action onFinish)
        {
            // Debug.Log($"UpdateGameState begin {debugState}");

            if (nodeChanged)
            {
                // Debug.Log($"Nova: Node changed to {currentNode.name}");

                this.nodeChanged.Invoke(new NodeChangedData(currentNode.name));

                if (firstEntryOfNode)
                {
                    forceCheckpoint = true; // Always get a checkpoint at the beginning of the node
                }
            }

            if (dialogueChanged)
            {
                this.RuntimeAssert(
                    currentIndex >= 0 && (currentNode.dialogueEntryCount == 0 ||
                                          currentIndex < currentNode.dialogueEntryCount),
                    "Dialogue index out of range.");
                if (currentNode.dialogueEntryCount > 0)
                {
                    currentDialogueEntry = currentNode.GetDialogueEntryAt(currentIndex);
                    ExecuteAction(UpdateDialogue(firstEntryOfNode, dialogueStepped, isCheckpoint, onFinish));
                }
                else
                {
                    StepAtEndOfNode(onFinish);
                }
            }
            else
            {
                onFinish?.Invoke();
            }

            // Debug.Log($"UpdateGameState end {debugState}");
        }

        private void ExecuteAction(IEnumerator coroutine)
        {
            ResetActionContext();
            state = State.ActionRunning;
            actionCoroutine = StartCoroutine(coroutine);
        }

        private IEnumerator UpdateDialogue(bool firstEntryOfNode, bool dialogueStepped, bool isCheckpoint,
            Action onFinish)
        {
            if (!isCheckpoint)
            {
                // If the following two lines of code are put into a new coroutine function, one frame delay will be introduced,
                // so don't do that
                currentDialogueEntry.ExecuteAction(DialogueActionStage.BeforeCheckpoint, isMovingBack);
                while (actionPauseLock.isLocked) yield return null;
            }

            DialogueSaveCheckpoint(firstEntryOfNode, dialogueStepped, out var hasBeenReached,
                out var hasBeenReachedWithAnyHistory);
            dialogueWillChange.Invoke(new DialogueWillChangeData());

            currentDialogueEntry.ExecuteAction(DialogueActionStage.Default, isMovingBack);
            while (actionPauseLock.isLocked) yield return null;

            // Everything that makes game state pause has ended, so change dialogue
            // TODO: use advancedDialogueHelper to override dialogue
            // The game author should define overriding dialogues for each locale
            // By the way, we don't need to store all dialogues in save data,
            // just those overridden
            dialogueChanged.Invoke(new DialogueChangedData(nodeHistory.GetCounted(currentNode.name), currentIndex,
                currentDialogueEntry.displayData, new Dictionary<string, VoiceEntry>(voicesOfNextDialogue),
                hasBeenReached, hasBeenReachedWithAnyHistory));

            voicesOfNextDialogue.Clear();

            currentDialogueEntry.ExecuteAction(DialogueActionStage.AfterDialogue, isMovingBack);
            while (actionPauseLock.isLocked) yield return null;

            state = State.Normal;

            if (advancedDialogueHelper.GetFallThrough())
            {
                Step(_ => onFinish?.Invoke());
                yield break;
            }

            var pendingJumpTarget = advancedDialogueHelper.GetJump();
            if (pendingJumpTarget != null)
            {
                var node = flowChartTree.GetNode(pendingJumpTarget);
                this.RuntimeAssert(node != null, $"Node {pendingJumpTarget} does not exist!");
                MoveToNextNode(node, onFinish);
                yield break;
            }

            onFinish?.Invoke();
        }

        private IEnumerator DoBranch(IEnumerable<BranchInformation> branchInfos, Action onFinish)
        {
            foreach (var branchInfo in branchInfos)
            {
                if (branchInfo.mode == BranchMode.Jump)
                {
                    if (branchInfo.condition == null || branchInfo.condition.Invoke<bool>())
                    {
                        SelectBranch(branchInfo.name, onFinish);
                        yield break;
                    }
                }
            }

            var selections = new List<SelectionOccursData.Selection>();
            var selectionNames = new List<string>();

            foreach (var branchInfo in branchInfos)
            {
                if (branchInfo.mode == BranchMode.Jump)
                {
                    continue;
                }

                if (branchInfo.mode == BranchMode.Show && !branchInfo.condition.Invoke<bool>())
                {
                    continue;
                }

                var selection = new SelectionOccursData.Selection(branchInfo.texts, branchInfo.imageInfo,
                    active: branchInfo.mode != BranchMode.Enable || branchInfo.condition.Invoke<bool>());
                selections.Add(selection);
                selectionNames.Add(branchInfo.name);
            }

            RaiseSelection(selections);

            while (coroutineHelper.fence == null)
            {
                yield return null;
            }

            var index = (int)coroutineHelper.Take();
            SelectBranch(selectionNames[index], onFinish);
        }

        private void DialogueSaveCheckpoint(bool firstEntryOfNode, bool dialogueStepped, out bool hasBeenReached,
            out bool hasBeenReachedWithAnyHistory)
        {
            if (!firstEntryOfNode && dialogueStepped)
            {
                stepNumFromLastCheckpoint++;
            }

            var entry = checkpointManager.GetReached(nodeHistory, currentIndex);
            hasBeenReached = entry != null;
            hasBeenReachedWithAnyHistory = checkpointManager.IsReachedWithAnyHistory(currentNode.name, currentIndex);
            if (entry == null)
            {
                // Tell the checkpoint manager a new dialogue entry has been reached
                checkpointManager.SetReached(nodeHistory, currentIndex, GetRestoreEntry());
            }

            // Change states after creating or restoring from checkpoint
            if (shouldSaveCheckpoint)
            {
                stepNumFromLastCheckpoint = 0;
            }

            if (entry != null)
            {
                this.RuntimeAssert(stepNumFromLastCheckpoint == entry.stepNumFromLastCheckpoint,
                    $"stepNumFromLastCheckpoint mismatch: {stepNumFromLastCheckpoint} {entry.stepNumFromLastCheckpoint}. Try to clear save data.");
                this.RuntimeAssert(restrainCheckpointNum == entry.restrainCheckpointNum,
                    $"restrainCheckpointNum mismatch: {restrainCheckpointNum} {entry.restrainCheckpointNum}. Try to clear save data.");
            }

            if (checkpointRestrained)
            {
                restrainCheckpointNum--;
            }

            // As the action for this dialogue will be rerun, it's fine to just reset _forceCheckpoint to false
            forceCheckpoint = false;
        }

        private AdvancedDialogueHelper advancedDialogueHelper;
        private CoroutineHelper coroutineHelper;

        /// <summary>
        /// Move on to the next node
        /// </summary>
        /// <param name="nextNode">The next node to move to</param>
        /// <param name="onFinish">Callback on finish</param>
        private void MoveToNextNode(FlowChartNode nextNode, Action onFinish)
        {
            nodeHistory.Add(nextNode.name);
            currentNode = nextNode;
            currentIndex = 0;
            UpdateGameState(true, true, true, true, false, onFinish);
        }

        public bool isMovingBack { get; private set; }

        /// <summary>
        /// Move back to a previously stepped node, the lazy execution block and callbacks at the target dialogue entry
        /// will be executed again.
        /// </summary>
        /// <param name="nodeHistoryEntry">The node name and the visit count to move to</param>
        /// <param name="dialogueIndex">The index of the dialogue to move to</param>
        /// <param name="clearFuture">Clear saved checkpoints in the future</param>
        /// <param name="onFinish">Callback on finish</param>
        public void MoveBackTo(NodeHistoryEntry nodeHistoryEntry, int dialogueIndex, bool clearFuture = false,
            Action onFinish = null)
        {
            // Debug.Log($"MoveBackTo begin {nodeHistoryEntry.Key} {nodeHistoryEntry.Value} {dialogueIndex}");

            CancelAction();

            // Animation should stop
            NovaAnimation.StopAll(AnimationType.All ^ AnimationType.UI);

            // Restore history
            var backNodeIndex = nodeHistory.FindLastIndex(x => x.Equals(nodeHistoryEntry));
            if (backNodeIndex < 0)
            {
                Debug.LogWarning($"Nova: Move back to node {nodeHistoryEntry.Key} that has not been walked through.");
            }

            // State should be normal when goes backward
            state = State.Normal;

            if (clearFuture)
            {
                // All save data of nodes to be removed are deleted
                for (var i = backNodeIndex + 1; i < nodeHistory.Count; ++i)
                {
                    checkpointManager.UnsetReached(nodeHistory.GetHash(0, i + 1));
                }

                // All save data of later dialogues are deleted
                for (var i = dialogueIndex + 1; i < flowChartTree.GetNode(nodeHistoryEntry.Key).dialogueEntryCount; ++i)
                {
                    checkpointManager.UnsetReached(nodeHistory, i);
                }
            }

            nodeHistory.RemoveRange(backNodeIndex + 1, nodeHistory.Count - (backNodeIndex + 1));
            if (backNodeIndex < 0)
            {
                nodeHistory.Add(nodeHistoryEntry.Key);
            }

            currentNode = flowChartTree.GetNode(nodeHistoryEntry.Key);
            currentIndex = dialogueIndex;

            // Restore data
            var entry = checkpointManager.GetReached(nodeHistory, dialogueIndex);
            this.RuntimeAssert(entry != null, $"Unable to find node with nodeHistory {nodeHistory}");

            Restore(entry, onFinish);

            // Debug.Log($"MoveBackTo end {nodeHistoryEntry.Key} {nodeHistoryEntry.Value} {dialogueIndex}");
        }

        #region Game start

        public void SaveInitialState()
        {
            // Save a clean state of game scene
            if (clearSceneRestoreEntry == null)
            {
                clearSceneRestoreEntry = GetCheckpoint();
            }
        }

        /// <summary>
        /// Start the game from the given node
        /// </summary>
        /// <param name="startNode">The node from where the game starts</param>
        private void GameStart(FlowChartNode startNode)
        {
            // Clear possible history
            nodeHistory.Clear();
            state = State.Normal;
            MoveToNextNode(startNode, () => { });
        }

        /// <summary>
        /// Start the game from the default start point
        /// </summary>
        public void GameStart()
        {
            CancelAction();
            var startNode = flowChartTree.defaultStartNode;
            GameStart(startNode);
        }

        /// <summary>
        /// Start the game from a named start point
        /// </summary>
        /// <param name="startName">the name of the start</param>
        public void GameStart(string startName)
        {
            CancelAction();
            var startNode = flowChartTree.GetStartNode(startName);
            GameStart(startNode);
        }

        public List<string> GetAllStartNodeNames()
        {
            return flowChartTree.GetAllStartNodeNames();
        }

        public List<string> GetAllUnlockedStartNodeNames()
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

        public void Step()
        {
            Step(_ => { });
        }

        /// <summary>
        /// Step to the next dialogue entry
        /// </summary>
        /// <remarks>
        /// This method runs asynchronously. The callback will run when the step finishes
        /// </remarks>
        /// <param name="onFinish">(canStepForward) => { ... }</param>
        /// <returns>True if successfully stepped to the next dialogue or trigger some events</returns>
        public void Step(Action<bool> onFinish)
        {
            if (!canStepForward)
            {
                onFinish?.Invoke(false);
                return;
            }

            var successCallback = new Action(() => { onFinish?.Invoke(true); });

            // If the next dialogue entry is in the current node, directly step to it
            if (currentIndex + 1 < currentNode.dialogueEntryCount)
            {
                ++currentIndex;
                UpdateGameState(false, true, false, true, false, successCallback);
            }
            else
            {
                StepAtEndOfNode(successCallback);
            }
        }

        private void StepAtEndOfNode(Action onFinish)
        {
            switch (currentNode.type)
            {
                case FlowChartNodeType.Normal:
                    MoveToNextNode(currentNode.next, onFinish);
                    break;
                case FlowChartNodeType.Branching:
                    ExecuteAction(DoBranch(currentNode.GetAllBranches(), onFinish));
                    break;
                case FlowChartNodeType.End:
                    state = State.Ended;
                    var endName = flowChartTree.GetEndName(currentNode);
                    if (!checkpointManager.IsReached(endName))
                    {
                        checkpointManager.SetReached(endName);
                    }

                    currentRouteEnded.Invoke(new CurrentRouteEndedData(endName));
                    onFinish?.Invoke();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void RaiseSelection(List<SelectionOccursData.Selection> selections)
        {
            selectionOccurs.Invoke(new SelectionOccursData(selections));
        }

        /// <summary>
        /// Call this method to choose a branch
        /// </summary>
        /// <remarks>
        /// This method should be called when a branch happens, otherwise An InvalidAccessException will be raised
        /// </remarks>
        /// <param name="branchName">The name of the branch</param>
        /// <param name="onFinish">Callback on finish</param>
        /// <exception cref="InvalidAccessException">An InvalidAccessException will be thrown if this method
        /// is not called on branch happens</exception>
        private void SelectBranch(string branchName, Action onFinish)
        {
            state = State.Normal;
            var nextNode = currentNode.GetNext(branchName);
            if (!checkpointManager.IsReached(nodeHistory, branchName))
            {
                // Tell the checkpoint manager that the branch has been selected
                checkpointManager.SetReached(nodeHistory, branchName);
            }

            MoveToNextNode(nextNode, onFinish);
        }

        #region Restoration

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
                throw new ArgumentException("Nova: A restorable should have an unique and non-null name.", ex);
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

        public const int WarningStepNumFromLastCheckpoint = 100;

        /// <remarks>
        /// Modified by DialogueSaveCheckpoint(), EnsureCheckpoint() and RestoreCheckpoint()
        /// </remarks>
        private int stepNumFromLastCheckpoint;

        /// <summary>
        /// Restrain saving checkpoints.
        /// This feature is necessary for restoring persistent animations.
        /// This restraint has higher priority than EnsureCheckpoint().
        /// </summary>
        /// <remarks>
        /// Modified by DialogueSaveCheckpoint(), RestrainCheckpoint() and RestoreCheckpoint()
        /// </remarks>
        private int restrainCheckpointNum;

        private bool checkpointRestrained => restrainCheckpointNum > 0;

        /// <summary>
        /// Restrain saving checkpoints for given steps. Force overwrite the number of restraining steps when overridden is true.
        /// </summary>
        /// <param name="steps">the steps to restrain checkpoints</param>
        /// <param name="overridden">if the new restraining step num should overwrite the old one</param>
        public void RestrainCheckpoint(int steps, bool overridden = false)
        {
            // check overwrite
            if (!overridden && restrainCheckpointNum >= steps) return;
            // non-negative
            if (steps < 0) steps = 0;
            restrainCheckpointNum = steps;
        }

        /// <summary>
        /// Used to force save a checkpoint before a persistent animation begins.
        /// </summary>
        /// <remarks>
        /// Modified by DialogueSaveCheckpoint(), EnsureCheckpointOnNextDialogue() and RestoreCheckpoint()
        /// </remarks>
        private bool forceCheckpoint = false;

        public void EnsureCheckpointOnNextDialogue()
        {
            forceCheckpoint = true;
        }

        private bool shouldSaveCheckpoint => forceCheckpoint ||
                                             (!checkpointRestrained && stepNumFromLastCheckpoint >=
                                                 maxStepNumFromLastCheckpoint);

        /// <summary>
        /// Force to get the current game state as a checkpoint
        /// </summary>
        /// <returns>current game state checkpoint</returns>
        private GameStateCheckpoint GetCheckpoint()
        {
            var restoreDatas = new Dictionary<string, IRestoreData>();
            foreach (var restorable in restorables)
            {
                restoreDatas[restorable.Key] = restorable.Value.GetRestoreData();
            }

            return new GameStateCheckpoint(restoreDatas, variables, restrainCheckpointNum);
        }

        /// <summary>
        /// Get all restore data from registered restorables
        /// </summary>
        /// <returns>a game state step restore entry that contains all restore datas for the current dialogue</returns>
        private GameStateRestoreEntry GetRestoreEntry()
        {
            // Debug.Log($"GetRestoreEntry {debugState}");

            if (shouldSaveCheckpoint)
            {
                return GetCheckpoint();
            }

            return new GameStateSimpleEntry(stepNumFromLastCheckpoint, restrainCheckpointNum);
        }

        private void RestoreCheckpoint(GameStateCheckpoint entry)
        {
            this.RuntimeAssert(entry != null, "Checkpoint is null");

            stepNumFromLastCheckpoint = 0;
            restrainCheckpointNum = entry.restrainCheckpointNum;
            forceCheckpoint = false;

            variables.CopyFrom(entry.variables);

            foreach (var pair in
                     from pair in restorables
                     orderby (pair.Value as IPrioritizedRestorable)?.priority ?? RestorablePriority.Normal descending
                     select pair)
            {
                try
                {
                    var restoreData = entry.restoreDatas[pair.Key];
                    pair.Value.Restore(restoreData);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogWarningFormat("Nova: Key {0} not found in restoreDatas. Check if the restorable name " +
                                           "has changed. If that is true, try clear all checkpoint " +
                                           "files, or undo the change of the restorable name.", pair.Key);
                }
            }
        }

        /// <summary>
        /// Get the node name, the visit count, and the dialogue index before specified steps
        /// </summary>
        /// <param name="steps">number to step back</param>
        /// <param name="nodeHistoryEntry">node name and visit count at given steps before</param>
        /// <param name="dialogueIndex">dialogue index at given steps before</param>
        /// <returns>true when success, false when step is too large or a minus number</returns>
        public bool SeekBackStep(int steps, out NodeHistoryEntry nodeHistoryEntry, out int dialogueIndex)
        {
            if (steps < 0)
            {
                nodeHistoryEntry = new NodeHistoryEntry("", -1);
                dialogueIndex = -1;
                return false;
            }

            if (currentIndex >= steps)
            {
                nodeHistoryEntry = nodeHistory.Last();
                dialogueIndex = currentIndex - steps;
                return true;
            }

            // The following code won't be frequently executed, since there is always a checkpoint at the
            // start of the node, and the step number in GameStateRestoreEntry never steps across node
            // boundary.

            steps -= currentIndex;
            for (var i = nodeHistory.Count - 2; i >= 0; i--)
            {
                var node = flowChartTree.GetNode(nodeHistory[i].Key);
                if (node.dialogueEntryCount >= steps)
                {
                    nodeHistoryEntry = nodeHistory[i];
                    dialogueIndex = node.dialogueEntryCount - steps;
                    return true;
                }

                steps -= node.dialogueEntryCount;
            }

            nodeHistoryEntry = new NodeHistoryEntry("", -1);
            dialogueIndex = -1;
            return false;
        }

        /// <summary>
        /// Restore all restorables. The lazy execution block in the target entry will be executed again.
        /// </summary>
        /// <param name="restoreData">Data to restore</param>
        /// <param name="onFinish">Callback on finish</param>
        private void Restore(GameStateRestoreEntry restoreData, Action onFinish)
        {
            if (restoreData is GameStateCheckpoint checkpointEntry)
            {
                RestoreCheckpoint(checkpointEntry);
                UpdateGameState(true, true, false, false, true, onFinish);
            }
            else if (restoreData is GameStateSimpleEntry simpleEntry)
            {
                if (!SeekBackStep(simpleEntry.stepNumFromLastCheckpoint, out NodeHistoryEntry storedNode,
                        out int storedDialogueIndex))
                {
                    Debug.LogErrorFormat("Nova: Failed to seek back, invalid stepNumFromLastCheckpoint: {0}",
                        simpleEntry.stepNumFromLastCheckpoint);
                }

                isMovingBack = true;

                void Callback(int i, int stepCount)
                {
                    var isLast = i == stepCount - 1;
                    if (isLast)
                    {
                        isMovingBack = false;
                    }

                    NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                    Step(_ =>
                    {
                        if (isLast)
                        {
                            onFinish?.Invoke();
                        }
                        else
                        {
                            Callback(i + 1, stepCount);
                        }
                    });
                }

                MoveBackTo(storedNode, storedDialogueIndex, false,
                    () => Callback(0, simpleEntry.stepNumFromLastCheckpoint));
            }
            else
            {
                throw new ArgumentException($"Nova: {restoreData} is not supported.");
            }
        }

        #endregion

        #region Bookmark

        /// <summary>
        /// Get the Bookmark of current state
        /// </summary>
        public Bookmark GetBookmark()
        {
            return new Bookmark(nodeHistory, currentIndex);
        }

        /// <summary>
        /// Load a Bookmark, restore to the saved state
        /// </summary>
        public void LoadBookmark(Bookmark bookmark)
        {
            CancelAction();
            nodeHistory.Clear();
            nodeHistory.AddRange(checkpointManager.GetNodeHistory(bookmark.nodeHistoryHash));
            MoveBackTo(nodeHistory.Last(), bookmark.dialogueIndex);
        }

        #endregion

        private string debugState =>
            $"{currentNode.name} {currentIndex} {variables.hash} | {stepNumFromLastCheckpoint} {restrainCheckpointNum} {forceCheckpoint} {shouldSaveCheckpoint}";
    }
}