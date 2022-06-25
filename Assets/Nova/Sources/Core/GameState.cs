using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
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
        public readonly IReadOnlyDictionary<string, VoiceEntry> voicesNextDialogue;
        public readonly bool isReached;
        public readonly bool isReachedAnyHistory;

        public DialogueChangedData(NodeHistoryEntry nodeHistoryEntry, int dialogueIndex,
            DialogueDisplayData displayData, IReadOnlyDictionary<string, VoiceEntry> voicesNextDialogue, bool isReached,
            bool isReachedAnyHistory)
        {
            this.nodeHistoryEntry = nodeHistoryEntry;
            this.dialogueIndex = dialogueIndex;
            this.displayData = displayData;
            this.voicesNextDialogue = voicesNextDialogue;
            this.isReached = isReached;
            this.isReachedAnyHistory = isReachedAnyHistory;
        }
    }

    [Serializable]
    public class DialogueChangedEvent : UnityEvent<DialogueChangedData> { }

    public class NodeChangedData
    {
        public readonly NodeHistoryEntry nodeHistoryEntry;

        public NodeChangedData(NodeHistoryEntry nodeHistoryEntry)
        {
            this.nodeHistoryEntry = nodeHistoryEntry;
        }
    }

    [Serializable]
    public class NodeChangedEvent : UnityEvent<NodeChangedData> { }

    public class SelectionOccursData
    {
        [ExportCustomType]
        public class Selection
        {
            public readonly IReadOnlyDictionary<SystemLanguage, string> texts;
            public readonly BranchImageInformation imageInfo;
            public readonly bool active;

            public Selection(IReadOnlyDictionary<SystemLanguage, string> texts, BranchImageInformation imageInfo,
                bool active)
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

        public readonly IReadOnlyList<Selection> selections;

        public SelectionOccursData(IReadOnlyList<Selection> selections)
        {
            this.selections = selections;
        }
    }

    [Serializable]
    public class SelectionOccursEvent : UnityEvent<SelectionOccursData> { }

    public class RouteEndedData
    {
        public readonly string endName;

        public RouteEndedData(string endName)
        {
            this.endName = endName;
        }
    }

    [Serializable]
    public class RouteEndedEvent : UnityEvent<RouteEndedData> { }

    #endregion

    /// <inheritdoc />
    /// <summary>
    /// This class manages the AVG game state.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        [SerializeField] private string scriptPath;

        private CheckpointManager checkpointManager;
        private GameStateCheckpoint initialCheckpoint;
        private readonly ScriptLoader scriptLoader = new ScriptLoader();
        private FlowChartTree flowChartTree;

        private AdvancedDialogueHelper advancedDialogueHelper;
        private CoroutineHelper coroutineHelper;

        private void Awake()
        {
            try
            {
                scriptLoader.Init(scriptPath);
                flowChartTree = scriptLoader.GetFlowChartTree();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
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
        /// Used for faster iterations during the development, like to modify scripts without restarting the game
        /// </summary>
        public void ReloadScripts()
        {
            LuaRuntime.Instance.Reset();
            scriptLoader.ForceInit(scriptPath);
            flowChartTree = scriptLoader.GetFlowChartTree();
        }

        #region States

        /// <summary>
        /// Names and visit counts of the nodes that have been walked through, including the current node
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
        /// Variables saved by Nova
        /// </summary>
        public readonly Variables variables = new Variables();

        private enum State
        {
            Normal,
            Ended
        }

        private State state = State.Normal;

        /// <summary>
        /// Reset GameState as if the game is not started
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
            variables.Clear();
            state = State.Normal;

            // Restore scene
            if (initialCheckpoint != null)
            {
                RestoreCheckpoint(initialCheckpoint);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// This event will be triggered if the content of the dialogue will change. It will be triggered before
        /// the lazy execution block of the new dialogue is invoked.
        /// </summary>
        public DialogueWillChangeEvent dialogueWillChange;

        /// <summary>
        /// This event will be triggered if the content of the dialogue has changed. The new dialogue text will be
        /// sent to all listeners.
        /// </summary>
        public DialogueChangedEvent dialogueChangedEarly;

        public DialogueChangedEvent dialogueChanged;

        /// <summary>
        /// This event will be triggered if the node has changed. The new node name will be sent to all listeners.
        /// </summary>
        public NodeChangedEvent nodeChanged;

        /// <summary>
        /// This event will be triggered if a selection occurs, either when branches occur or when a selection is
        /// triggered from the script.
        /// </summary>
        public SelectionOccursEvent selectionOccurs;

        /// <summary>
        /// This event will be triggered if the story route has reached an end.
        /// </summary>
        public RouteEndedEvent routeEnded;

        #endregion

        #region Voice

        private readonly Dictionary<string, VoiceEntry> voicesNextDialogue = new Dictionary<string, VoiceEntry>();

        /// <summary>
        /// Add a voice clip to be played on the next dialogue
        /// </summary>
        /// <remarks>
        /// This method is called by CharacterController
        /// </remarks>
        public void AddVoiceNextDialogue(string characterName, VoiceEntry voiceEntry)
        {
            voicesNextDialogue.Add(characterName, voiceEntry);
        }

        #endregion

        #region Action coroutine

        private Coroutine actionCoroutine;

        private void ExecuteAction(IEnumerator coroutine)
        {
            ResetActionContext();
            actionCoroutine = StartCoroutine(coroutine);
        }

        private void CancelAction()
        {
            if (actionCoroutine != null)
            {
                StopCoroutine(actionCoroutine);
                actionCoroutine = null;
            }

            DialogueEntry.StopActionCoroutine();

            ResetActionContext();
        }

        private void ResetActionContext()
        {
            voicesNextDialogue.Clear();
            advancedDialogueHelper.Reset();
            coroutineHelper.Reset();
            actionPauseLock.Reset();
        }

        /// <summary>
        /// Acquiring this lock will make GameState pause and wait for the lock being released
        /// </summary>
        private readonly CounterLock actionPauseLock = new CounterLock();

        /// <summary>
        /// Methods invoked from lazy execution blocks can acquire GameState to pause, i.e., the dialogue changes
        /// only after those methods release the pause
        /// </summary>
        public void AcquireActionPause()
        {
            actionPauseLock.Acquire();
        }

        /// <summary>
        /// Methods that acquire GameState to pause should release the pause when their actions finish
        /// </summary>
        public void ReleaseActionPause()
        {
            actionPauseLock.Release();
        }

        private ulong variablesHashBeforeInterrupt;

        public void StartInterrupt()
        {
            variablesHashBeforeInterrupt = variables.hash;
        }

        public void StopInterrupt()
        {
            if (variables.hash != variablesHashBeforeInterrupt)
            {
                nodeHistory.AddInterrupt(currentIndex, variables);
            }
        }

        public void SignalFence(object value)
        {
            coroutineHelper.SignalFence(value);
        }

        #endregion

        #region Update game state

        /// <summary>
        /// Called after the current node or the current dialogue index has changed
        /// </summary>
        /// <remarks>
        /// Trigger events according to the current states and how they were changed
        /// </remarks>
        private void UpdateGameState(bool nodeChanged, bool dialogueChanged, bool firstEntryOfNode,
            bool dialogueStepped, bool fromCheckpoint, Action onFinish)
        {
            // Debug.Log($"UpdateGameState begin {debugState}");

            if (nodeChanged)
            {
                // Debug.Log($"Node changed to {currentNode.name}");

                this.nodeChanged.Invoke(new NodeChangedData(nodeHistory.Last()));

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
                    ExecuteAction(UpdateDialogue(firstEntryOfNode, dialogueStepped, fromCheckpoint, onFinish));
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

        private IEnumerator UpdateDialogue(bool firstEntryOfNode, bool dialogueStepped, bool fromCheckpoint,
            Action onFinish)
        {
            if (!fromCheckpoint)
            {
                // If the following lines of code are put into a new coroutine, one frame's delay will be introduced,
                // so don't do that
                currentDialogueEntry.ExecuteAction(DialogueActionStage.BeforeCheckpoint, isRestoring);
                while (actionPauseLock.isLocked) yield return null;
            }

            DialogueSaveCheckpoint(firstEntryOfNode, dialogueStepped, out var isReached,
                out var isReachedAnyHistory);
            dialogueWillChange.Invoke(new DialogueWillChangeData());

            currentDialogueEntry.ExecuteAction(DialogueActionStage.Default, isRestoring);
            while (actionPauseLock.isLocked) yield return null;

            var dialogueChangedData = new DialogueChangedData(nodeHistory.Last(), currentIndex,
                currentDialogueEntry.GetDisplayData(), new Dictionary<string, VoiceEntry>(voicesNextDialogue),
                isReached, isReachedAnyHistory);
            dialogueChangedEarly.Invoke(dialogueChangedData);
            dialogueChanged.Invoke(dialogueChangedData);

            voicesNextDialogue.Clear();

            currentDialogueEntry.ExecuteAction(DialogueActionStage.AfterDialogue, isRestoring);
            while (actionPauseLock.isLocked) yield return null;

            if (advancedDialogueHelper.GetFallThrough())
            {
                Step(Utils.WrapActionWithParameter<bool>(onFinish));
                yield break;
            }

            var pendingJumpTarget = advancedDialogueHelper.GetJump();
            if (pendingJumpTarget != null)
            {
                var node = flowChartTree.GetNode(pendingJumpTarget);
                this.RuntimeAssert(node != null, $"Node {pendingJumpTarget} not found.");
                MoveToNextNode(node, onFinish);
                yield break;
            }

            onFinish?.Invoke();
        }

        private void DialogueSaveCheckpoint(bool firstEntryOfNode, bool dialogueStepped, out bool isReached,
            out bool isReachedAnyHistory)
        {
            if (!firstEntryOfNode && dialogueStepped)
            {
                stepNumFromLastCheckpoint++;
            }

            var entry = checkpointManager.GetReached(nodeHistory, currentIndex);
            isReached = entry != null;
            isReachedAnyHistory = checkpointManager.IsReachedAnyHistory(currentNode.name, currentIndex);
            if (entry == null)
            {
                // Tell CheckpointManager that a new dialogue entry has been reached
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

            // As the action for this dialogue will be re-run, it's fine to just reset forceCheckpoint to false
            forceCheckpoint = false;
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
                    if (!checkpointManager.IsEndReached(endName))
                    {
                        checkpointManager.SetEndReached(endName);
                    }

                    routeEnded.Invoke(new RouteEndedData(endName));
                    onFinish?.Invoke();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MoveToNextNode(FlowChartNode nextNode, Action onFinish)
        {
            nodeHistory.Add(nextNode.name);
            currentNode = nextNode;
            currentIndex = 0;
            UpdateGameState(true, true, true, true, false, onFinish);
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

            AcquireActionPause();

            RaiseSelections(selections);
            while (coroutineHelper.fence == null)
            {
                yield return null;
            }

            ReleaseActionPause();

            var index = (int)coroutineHelper.TakeFence();
            SelectBranch(selectionNames[index], onFinish);
        }

        private void SelectBranch(string branchName, Action onFinish)
        {
            var nextNode = currentNode.GetNext(branchName);
            if (!checkpointManager.IsBranchReached(nodeHistory, branchName))
            {
                // Tell the checkpoint manager that the branch has been selected
                checkpointManager.SetBranchReached(nodeHistory, branchName);
            }

            MoveToNextNode(nextNode, onFinish);
        }

        #endregion

        #region Game start

        public void SaveInitialCheckpoint()
        {
            // Save a clean state of game scene
            if (initialCheckpoint == null)
            {
                initialCheckpoint = GetCheckpoint();
            }
        }

        /// <summary>
        /// Start the game from the given node
        /// </summary>
        private void GameStart(FlowChartNode startNode)
        {
            ResetGameState();
            MoveToNextNode(startNode, null);
        }

        /// <summary>
        /// Start the game from the default start point
        /// </summary>
        public void GameStart()
        {
            GameStart(flowChartTree.defaultStartNode);
        }

        /// <summary>
        /// Start the game from a named start point
        /// </summary>
        public void GameStart(string startName)
        {
            GameStart(flowChartTree.GetStartNode(startName));
        }

        public FlowChartNode GetNode(string name)
        {
            return flowChartTree.GetNode(name);
        }

        public IReadOnlyList<string> GetAllStartNodeNames()
        {
            return flowChartTree.GetAllStartNodeNames();
        }

        public IReadOnlyList<string> GetAllUnlockedStartNodeNames()
        {
            return flowChartTree.GetAllUnlockedStartNodeNames();
        }

        #endregion

        /// <summary>
        /// Check if the current state can step forward, i.e., something will happen when calling Step
        /// </summary>
        public bool canStepForward
        {
            get
            {
                if (currentNode == null)
                {
                    Debug.LogWarning("Nova: Cannot step forward before the game starts.");
                    return false;
                }

                if (state != State.Normal)
                {
                    return false;
                }

                if (actionPauseLock.isLocked)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Step to the next dialogue entry
        /// </summary>
        /// <remarks>
        /// This method can run asynchronously. The callback will be invoked when the step finishes.
        /// </remarks>
        /// <param name="onFinish">(canStepForward) => { ... }</param>
        public void Step(Action<bool> onFinish = null)
        {
            if (!canStepForward)
            {
                onFinish?.Invoke(false);
                return;
            }

            var successCallback = Utils.WrapActionWithoutParameter(onFinish, true);

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

        public void RaiseSelections(IReadOnlyList<SelectionOccursData.Selection> selections)
        {
            selectionOccurs.Invoke(new SelectionOccursData(selections));
        }

        #region Restoration

        /// <summary>
        /// All restorable objects
        /// </summary>
        private readonly Dictionary<string, IRestorable> restorables = new Dictionary<string, IRestorable>();

        /// <summary>
        /// Register a new restorable object
        /// </summary>
        /// <param name="restorable">The restorable to be added</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the name of the restorable object is null or duplicated
        /// </exception>
        public void AddRestorable(IRestorable restorable)
        {
            try
            {
                restorables.Add(restorable.restorableName, restorable);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("Nova: A restorable should have an unique and non-null name.", e);
            }
        }

        /// <summary>
        /// Unregister a restorable object
        /// </summary>
        /// <param name="restorable">The restorable to be removed</param>
        public void RemoveRestorable(IRestorable restorable)
        {
            restorables.Remove(restorable.restorableName);
        }

        /// <summary>
        /// Not all states of objects can be easily restored, like holding animations.
        /// We store some checkpoints, and other states can be restored by re-executing from the last checkpoint.
        /// At least one checkpoint will be saved every maxStepNumFromLastCheckpoint, except during holding animations.
        /// </summary>
        public int maxStepNumFromLastCheckpoint = 10;

        public const int WarningStepNumFromLastCheckpoint = 100;

        /// <remarks>
        /// Modified by DialogueSaveCheckpoint(), EnsureCheckpoint() and RestoreCheckpoint()
        /// </remarks>
        private int stepNumFromLastCheckpoint;

        /// <summary>
        /// Restrain saving checkpoints.
        /// </summary>
        /// <remarks>
        /// This feature is necessary for restoring holding animations.
        /// This restraint has higher priority than EnsureCheckpoint().
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
        /// Used to force save a checkpoint before a holding animation begins.
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
            this.RuntimeAssert(entry != null, "Checkpoint is null.");

            stepNumFromLastCheckpoint = 0;
            restrainCheckpointNum = entry.restrainCheckpointNum;
            forceCheckpoint = false;

            variables.CloneFrom(entry.variables);

            foreach (var pair in
                     from pair in restorables
                     orderby (pair.Value as IPrioritizedRestorable)?.priority ?? RestorablePriority.Normal descending
                     select pair)
            {
                try
                {
                    pair.Value.Restore(entry.restoreDatas[pair.Key]);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogWarning($"Nova: Key {pair.Key} not found in restoreDatas. Please clear save data.");
                }
            }
        }

        /// <summary>
        /// Get the node name, the visit count, and the dialogue index before specified steps
        /// </summary>
        /// <param name="steps">number to step back</param>
        /// <param name="nodeHistoryEntry">node name and visit count at given steps before</param>
        /// <param name="dialogueIndex">dialogue index at given steps before</param>
        /// <returns>true when succeed, false when steps is too large or a negative number</returns>
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
            // start of the node, and the step number in the restore entry never steps across node
            // boundaries.

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

        public bool isRestoring { get; private set; }

        /// <summary>
        /// Restore all restorables. The lazy execution block in the target entry will be executed again.
        /// </summary>
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
                    throw new ArgumentException(
                        $"Nova: Failed to seek back, invalid stepNumFromLastCheckpoint: {simpleEntry.stepNumFromLastCheckpoint}");
                }

                MoveBackAndFastForward(storedNode, storedDialogueIndex, simpleEntry.stepNumFromLastCheckpoint, false,
                    onFinish);
            }
            else
            {
                throw new ArgumentException($"Nova: {restoreData} is not supported.");
            }
        }

        public void MoveBackAndFastForward(NodeHistoryEntry nodeHistoryEntry, int dialogueIndex, int stepCount,
            bool clearFuture, Action onFinish)
        {
            isRestoring = true;
            MoveBackTo(nodeHistoryEntry, dialogueIndex, clearFuture);
            if (actionPauseLock.isLocked)
            {
                Debug.LogWarning("Nova: GameState paused by action when restoring.");
                isRestoring = false;
                return;
            }

            for (var i = 0; i < stepCount; ++i)
            {
                var isLast = i == stepCount - 1;
                if (isLast)
                {
                    isRestoring = false;
                }

                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                Step();
                if (actionPauseLock.isLocked)
                {
                    Debug.LogWarning("Nova: GameState paused by action when restoring.");
                    isRestoring = false;
                    return;
                }

                if (isLast)
                {
                    onFinish?.Invoke();
                }
            }
        }

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
                Debug.LogWarning($"Nova: Move back to node {nodeHistoryEntry.Key} that is not walked through.");
            }

            if (clearFuture)
            {
                // All save data of nodes to be removed are deleted
                for (var i = backNodeIndex + 1; i < nodeHistory.Count; ++i)
                {
                    checkpointManager.UnsetReached(nodeHistory.GetHashULong(0, i + 1));
                }

                // All save data of later dialogues are deleted
                checkpointManager.UnsetReachedAfter(nodeHistory, dialogueIndex);
            }

            nodeHistory.RemoveRange(backNodeIndex + 1, nodeHistory.Count - (backNodeIndex + 1));
            if (backNodeIndex < 0)
            {
                nodeHistory.Add(nodeHistoryEntry.Key);
            }

            nodeHistory.RemoveInterruptsAfter(backNodeIndex, dialogueIndex);

            currentNode = flowChartTree.GetNode(nodeHistoryEntry.Key);
            currentIndex = dialogueIndex;

            // Restore data
            var entry = checkpointManager.GetReached(nodeHistory, dialogueIndex);
            this.RuntimeAssert(entry != null,
                $"Unable to find restore entry with {nodeHistory} {nodeHistory.Hash}, dialogueIndex {dialogueIndex}");

            Restore(entry, onFinish);

            // Debug.Log($"MoveBackTo end {nodeHistoryEntry.Key} {nodeHistoryEntry.Value} {dialogueIndex}");
        }

        #endregion

        #region Bookmark

        /// <summary>
        /// Get the Bookmark for the current state
        /// </summary>
        public Bookmark GetBookmark()
        {
            return new Bookmark(nodeHistory, currentIndex);
        }

        /// <summary>
        /// Load a Bookmark and restore the saved state
        /// </summary>
        public void LoadBookmark(Bookmark bookmark)
        {
            checkpointManager.GetNodeHistory(bookmark.nodeHistoryHash, nodeHistory);
            MoveBackTo(nodeHistory.Last(), bookmark.dialogueIndex);
        }

        #endregion

        private string debugState =>
            $"{currentNode?.name} {currentIndex} {variables.hash} | {stepNumFromLastCheckpoint} {restrainCheckpointNum} {forceCheckpoint} {shouldSaveCheckpoint}";
    }
}