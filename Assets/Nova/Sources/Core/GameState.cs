using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    #region Event types and datas

    public class DialogueWillChangeData { }

    [Serializable]
    public class DialogueWillChangeEvent : UnityEvent<DialogueWillChangeData> { }

    public class DialogueChangedData
    {
        public readonly NodeRecord nodeRecord;
        public readonly long checkpointOffset;
        public readonly ReachedDialogueData dialogueData;
        public readonly DialogueDisplayData displayData;
        public readonly bool isReached;
        public readonly bool isReachedAnyHistory;

        public DialogueChangedData(NodeRecord nodeRecord, long checkpointOffset, ReachedDialogueData dialogueData,
            DialogueDisplayData displayData, bool isReached, bool isReachedAnyHistory)
        {
            this.nodeRecord = nodeRecord;
            this.checkpointOffset = checkpointOffset;
            this.dialogueData = dialogueData;
            this.displayData = displayData;
            this.isReached = isReached;
            this.isReachedAnyHistory = isReachedAnyHistory;
        }
    }

    [Serializable]
    public class DialogueChangedEvent : UnityEvent<DialogueChangedData> { }

    public class NodeChangedData
    {
        public readonly string newNode;

        public NodeChangedData(string newNode)
        {
            this.newNode = newNode;
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

            public Selection(string text, BranchImageInformation imageInfo, bool active) : this(
                new Dictionary<SystemLanguage, string> { [I18n.DefaultLocale] = text }, imageInfo, active)
            { }
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
        public FlowChartTree flowChartTree { get; private set; }

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
        // public readonly NodeHistory nodeHistory = new NodeHistory();
        private NodeRecord nodeRecord;

        private long checkpointOffset;

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
            nodeRecord = null;
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

        private readonly Dictionary<string, VoiceEntry> currentVoices = new Dictionary<string, VoiceEntry>();

        /// <summary>
        /// Add a voice clip to be played in the current dialogue
        /// </summary>
        /// <remarks>
        /// This method is called by GameCharacterController
        /// </remarks>
        public void AddVoice(string characterName, VoiceEntry voiceEntry)
        {
            currentVoices.Add(characterName, voiceEntry);
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
            currentVoices.Clear();
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
                AppendSameNode();
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
            bool dialogueStepped, bool fromCheckpoint)
        {
            // Debug.Log($"UpdateGameState begin {debugState}");

            if (nodeChanged)
            {
                // Debug.Log($"Node changed to {currentNode.name}");

                this.nodeChanged.Invoke(new NodeChangedData(nodeRecord.name));

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
                    ExecuteAction(UpdateDialogue(firstEntryOfNode, dialogueStepped, fromCheckpoint));
                }
                else
                {
                    StepAtEndOfNode();
                }
            }

            // Debug.Log($"UpdateGameState end {debugState}");
        }

        private IEnumerator UpdateDialogue(bool firstEntryOfNode, bool dialogueStepped, bool fromCheckpoint)
        {
            if (!fromCheckpoint)
            {
                // If the following lines of code are put into a new coroutine, one frame's delay will be introduced,
                // so don't do that
                currentDialogueEntry.ExecuteAction(DialogueActionStage.BeforeCheckpoint, isRestoring);
                while (actionPauseLock.isLocked) yield return null;
            }

            var isReached = DialogueSaveCheckpoint(firstEntryOfNode, dialogueStepped);
            dialogueWillChange.Invoke(new DialogueWillChangeData());

            currentDialogueEntry.ExecuteAction(DialogueActionStage.Default, isRestoring);
            while (actionPauseLock.isLocked) yield return null;

            var isReachedAnyHistory = DialogueSaveReachedData(out var dialogueData);
            var dialogueChangedData = new DialogueChangedData(nodeRecord, checkpointOffset, dialogueData,
                currentDialogueEntry.GetDisplayData(), isReached, isReachedAnyHistory);
            dialogueChangedEarly.Invoke(dialogueChangedData);
            dialogueChanged.Invoke(dialogueChangedData);

            currentDialogueEntry.ExecuteAction(DialogueActionStage.AfterDialogue, isRestoring);
            while (actionPauseLock.isLocked) yield return null;

            if (advancedDialogueHelper.GetFallThrough())
            {
                Step();
                yield break;
            }

            var pendingJumpTarget = advancedDialogueHelper.GetJump();
            if (pendingJumpTarget != null)
            {
                var node = flowChartTree.GetNode(pendingJumpTarget);
                this.RuntimeAssert(node != null, $"Node {pendingJumpTarget} not found.");
                MoveToNextNode(node);
            }
        }

        private void StepCheckpoint(bool isReached)
        {
            if (!isReached)
            {
                checkpointOffset = checkpointManager.AppendCheckpoint(currentIndex, GetCheckpoint());
            }
            else if (checkpointOffset == nodeRecord.offset)
            {
                checkpointOffset = checkpointManager.NextRecord(checkpointOffset);
            }
            else
            {
                checkpointOffset = checkpointManager.NextCheckpoint(checkpointOffset);
            }
        }

        private bool DialogueSaveCheckpoint(bool firstEntryOfNode, bool dialogueStepped)
        {
            if (!firstEntryOfNode && dialogueStepped)
            {
                stepNumFromLastCheckpoint++;
            }

            if (shouldSaveCheckpoint && currentIndex >= nodeRecord.endDialogue &&
                !checkpointManager.CanAppendCheckpoint(checkpointOffset))
            {
                AppendSameNode();
            }

            var isReached = currentIndex < nodeRecord.endDialogue;
            if (shouldSaveCheckpoint)
            {
                stepNumFromLastCheckpoint = 0;
                StepCheckpoint(isReached);
            }

            if (!isReached)
            {
                checkpointManager.AppendDialogue(nodeRecord, currentIndex, shouldSaveCheckpoint);
            }

            if (checkpointRestrained)
            {
                restrainCheckpointNum--;
            }

            // As the action for this dialogue will be re-run, it's fine to just reset forceCheckpoint to false
            forceCheckpoint = false;

            return isReached;
        }

        private bool DialogueSaveReachedData(out ReachedDialogueData dialogueData)
        {
            var isReachedAnyHistory = checkpointManager.IsReachedAnyHistory(currentNode.name, currentIndex);

            if (!isReachedAnyHistory)
            {
                var voices = currentVoices.Count > 0 ? new Dictionary<string, VoiceEntry>(currentVoices) : null;
                dialogueData = new ReachedDialogueData(currentNode.name, currentIndex, voices,
                    currentDialogueEntry.NeedInterpolate());
                checkpointManager.SetReached(dialogueData);
            }
            else
            {
                dialogueData = checkpointManager.GetReachedDialogueData(currentNode.name, currentIndex);
            }

            return isReachedAnyHistory;
        }

        private void StepAtEndOfNode()
        {
            switch (currentNode.type)
            {
                case FlowChartNodeType.Normal:
                    MoveToNextNode(currentNode.next);
                    break;
                case FlowChartNodeType.Branching:
                    ExecuteAction(DoBranch(currentNode.GetAllBranches()));
                    break;
                case FlowChartNodeType.End:
                    state = State.Ended;
                    var endName = flowChartTree.GetEndName(currentNode);
                    checkpointManager.SetEndReached(endName);
                    routeEnded.Invoke(new RouteEndedData(endName));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AppendSameNode()
        {
            nodeRecord = checkpointManager.GetNextNode(nodeRecord, nodeRecord.name, variables, currentIndex);
            checkpointOffset = nodeRecord.offset;
            forceCheckpoint = true;
        }

        public void AddDeferredDialogueChunks(FlowChartNode node)
        {
            scriptLoader.AddDeferredDialogueChunks(node);
        }

        private void MoveToNextNode(FlowChartNode nextNode)
        {
            AddDeferredDialogueChunks(nextNode);
            nodeRecord = checkpointManager.GetNextNode(nodeRecord, nextNode.name, variables, 0);
            currentNode = nextNode;
            currentIndex = 0;
            checkpointOffset = nodeRecord.offset;
            UpdateGameState(true, true, true, true, false);
        }

        private IEnumerator DoBranch(IEnumerable<BranchInformation> branchInfos)
        {
            foreach (var branchInfo in branchInfos)
            {
                if (branchInfo.mode == BranchMode.Jump)
                {
                    if (branchInfo.condition == null || branchInfo.condition.Invoke<bool>())
                    {
                        SelectBranch(branchInfo.name);
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
            SelectBranch(selectionNames[index]);
        }

        private void SelectBranch(string branchName)
        {
            MoveToNextNode(currentNode.GetNext(branchName));
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
            MoveToNextNode(startNode);
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
        public void Step()
        {
            if (!canStepForward)
            {
                return;
            }

            // If the next dialogue entry is in the current node, directly step to it
            if (currentIndex + 1 < currentNode.dialogueEntryCount)
            {
                ++currentIndex;
                UpdateGameState(false, true, false, true, false);
            }
            else
            {
                StepAtEndOfNode();
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
        private bool forceCheckpoint;

        public void EnsureCheckpointOnNextDialogue()
        {
            forceCheckpoint = true;
        }

        private bool shouldSaveCheckpoint =>
            forceCheckpoint || (!checkpointRestrained && stepNumFromLastCheckpoint >= maxStepNumFromLastCheckpoint);

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

            return new GameStateCheckpoint(currentIndex, restoreDatas, variables, restrainCheckpointNum);
        }

        private void RestoreCheckpoint(GameStateCheckpoint entry)
        {
            this.RuntimeAssert(entry != null, "Checkpoint is null.");

            currentIndex = entry.dialogueIndex;
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

        private void SeekBackStep(int steps, IList<NodeRecord> nodeHistory, out long newCheckpointOffset,
            out int newDialogueIndex)
        {
            // Debug.Log($"stepback={steps}");

            nodeHistory.Clear();
            var hasLimit = steps != 0;
            var curNode = nodeRecord;
            nodeHistory.Add(curNode);
            var endDialogue = currentIndex;
            while (curNode.parent != 0 && (!hasLimit || steps > endDialogue - curNode.beginDialogue))
            {
                if (hasLimit)
                {
                    steps -= endDialogue - curNode.beginDialogue;
                }

                curNode = checkpointManager.GetNodeRecord(curNode.parent);
                nodeHistory.Add(curNode);
                endDialogue = curNode.endDialogue;
            }

            if (hasLimit && steps > endDialogue - curNode.beginDialogue)
            {
                steps = 0;
            }

            newDialogueIndex = curNode.beginDialogue + steps;
            newCheckpointOffset = checkpointManager.NextRecord(curNode.offset);
            var checkpointDialogue = checkpointManager.GetCheckpointDialogue(newCheckpointOffset);
            while (checkpointDialogue < curNode.lastCheckpointDialogue)
            {
                var nextCheckpoint = checkpointManager.NextCheckpoint(newCheckpointOffset);
                var nextCheckpointDialogue = checkpointManager.GetCheckpointDialogue(nextCheckpoint);
                if (nextCheckpointDialogue > newDialogueIndex)
                {
                    break;
                }

                newCheckpointOffset = nextCheckpoint;
            }

            // Debug.Log($"newNode=@{nodeHistory[0].offset} newCheckpointOffset=@{newCheckpointOffset} newDialogueIndex={newDialogueIndex}");
        }

        public bool SeekBackStep(int steps, out NodeRecord nodeRecord, out long newCheckpointOffset,
            out int newDialogueIndex)
        {
            var list = new List<NodeRecord>();
            SeekBackStep(steps, list, out newCheckpointOffset, out newDialogueIndex);
            nodeRecord = list[list.Count - 1];
            // TODO: not ideal
            return true;
        }

        public bool isRestoring { get; private set; }

        private void FastForward(int stepCount)
        {
            this.RuntimeAssert(stepCount > 0, $"Invalid stepCount {stepCount}.");

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
            }
        }

        public void MoveBackToFirstDialogue()
        {
            var entryNode = nodeRecord;
            while (entryNode.parent != 0 && entryNode.beginDialogue != 0)
            {
                entryNode = checkpointManager.GetNodeRecord(entryNode.parent);
            }

            MoveBackTo(entryNode, entryNode.offset, entryNode.beginDialogue);
        }

        public void MoveBackTo(NodeRecord newNodeRecord, long newCheckpointOffset, int dialogueIndex)
        {
            // Debug.Log($"MoveBackTo begin {nodeHistoryEntry.Key} {nodeHistoryEntry.Value} {dialogueIndex}");

            CancelAction();

            // Animation should stop
            NovaAnimation.StopAll(AnimationType.All ^ AnimationType.UI);

            // Restore history
            nodeRecord = newNodeRecord;
            checkpointOffset = newCheckpointOffset;
            if (checkpointOffset == nodeRecord.offset)
            {
                checkpointOffset = checkpointManager.NextRecord(checkpointOffset);
            }

            currentNode = flowChartTree.GetNode(nodeRecord.name);

            isRestoring = true;
            var checkpoint = checkpointManager.GetCheckpoint(checkpointOffset);
            RestoreCheckpoint(checkpoint);
            this.RuntimeAssert(dialogueIndex >= currentIndex,
                $"dialogueIndex {dialogueIndex} is before currentIndex {currentIndex}.");
            if (dialogueIndex == currentIndex)
            {
                isRestoring = false;
            }

            UpdateGameState(true, true, false, false, true);
            if (actionPauseLock.isLocked)
            {
                Debug.LogWarning("Nova: GameState paused by action when restoring.");
                isRestoring = false;
                return;
            }

            if (dialogueIndex > currentIndex)
            {
                FastForward(dialogueIndex - currentIndex);
            }

            // Debug.Log($"MoveBackTo end {nodeHistoryEntry.Key} {nodeHistoryEntry.Value} {dialogueIndex}");
        }

        public IEnumerable<ReachedDialoguePosition> GetDialogueHistory(int limit = 0)
        {
            if (nodeRecord == null)
            {
                yield break;
            }

            List<NodeRecord> nodeHistory = new List<NodeRecord>();
            SeekBackStep(limit, nodeHistory, out var curCheckpoint, out var curDialogue);

            for (var i = nodeHistory.Count - 1; i >= 0; i--)
            {
                var curNode = nodeHistory[i];
                if (i != nodeHistory.Count - 1)
                {
                    curCheckpoint = checkpointManager.NextRecord(curNode.offset);
                    curDialogue = curNode.beginDialogue;
                }

                var checkpointDialogue = checkpointManager.GetCheckpointDialogue(curCheckpoint);
                var endDialogue = i == 0 ? currentIndex : curNode.endDialogue;
                while (curDialogue < endDialogue)
                {
                    var curEndDialogue = endDialogue;
                    long nextCheckpoint = 0;
                    if (checkpointDialogue < curNode.lastCheckpointDialogue)
                    {
                        nextCheckpoint = checkpointManager.NextCheckpoint(curCheckpoint);
                        var nextCheckpointDialogue = checkpointManager.GetCheckpointDialogue(nextCheckpoint);
                        if (nextCheckpointDialogue < endDialogue)
                        {
                            curEndDialogue = nextCheckpointDialogue;
                        }
                    }

                    while (curDialogue < curEndDialogue)
                    {
                        yield return new ReachedDialoguePosition(curNode, curCheckpoint, curDialogue);
                        curDialogue++;
                    }

                    if (curDialogue < endDialogue)
                    {
                        checkpointDialogue = curEndDialogue;
                        curCheckpoint = nextCheckpoint;
                    }
                }
            }
        }

        #endregion

        #region Bookmark

        /// <summary>
        /// Get the Bookmark for the current state
        /// </summary>
        public Bookmark GetBookmark()
        {
            if (nodeRecord == null || checkpointOffset == nodeRecord.offset)
            {
                throw new InvalidOperationException("cannot save bookmark at this point");
            }

            return new Bookmark(nodeRecord, checkpointOffset, currentIndex);
        }

        /// <summary>
        /// Load a Bookmark and restore the saved state
        /// </summary>
        public void LoadBookmark(Bookmark bookmark)
        {
            MoveBackTo(checkpointManager.GetNodeRecord(bookmark.nodeOffset), bookmark.checkpointOffset,
                bookmark.dialogueIndex);
        }

        #endregion

        // private string debugState => $"{currentNode?.name} {currentIndex} {variables.hash} | {stepNumFromLastCheckpoint} {restrainCheckpointNum} {forceCheckpoint} {shouldSaveCheckpoint}";
    }
}
