using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    /// <inheritdoc />
    /// <summary>
    /// This class manages the AVG game state.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        [SerializeField] private string scriptPath;

        private readonly ScriptLoader scriptLoader = new ScriptLoader();
        private FlowChartGraph flowChartGraph;
        private CheckpointManager checkpointManager;
        private GameStateCheckpoint initialCheckpoint;

        private AdvancedDialogueHelper advancedDialogueHelper;
        private CoroutineHelper coroutineHelper;

        private void Awake()
        {
            try
            {
                scriptLoader.Init(scriptPath);
                flowChartGraph = scriptLoader.GetFlowChartGraph();

                checkpointManager = GetComponent<CheckpointManager>();
                checkpointManager.Init();
                CheckScriptUpgrade();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Utils.Quit();
            }

            LuaRuntime.Instance.BindObject("variables", variables);
            advancedDialogueHelper = new AdvancedDialogueHelper(this);
            LuaRuntime.Instance.BindObject("advancedDialogueHelper", advancedDialogueHelper);
            coroutineHelper = new CoroutineHelper(this);
            LuaRuntime.Instance.BindObject("coroutineHelper", coroutineHelper);
        }

        private void Start()
        {
            SaveInitialCheckpoint();
        }

        private void CheckScriptUpgrade()
        {
            var changedNodes = checkpointManager.CheckScriptUpgrade(scriptLoader, flowChartGraph);
            // Debug.Log($"upgrade {changedNodes.Count} nodes");
            if (changedNodes.Any())
            {
                var upgrader = new CheckpointUpgrader(this, checkpointManager, changedNodes);
                upgrader.UpgradeSaves();
            }
        }

        /// <summary>
        /// Used for faster iterations during the development, like to modify scripts without restarting the game
        /// </summary>
        public void ReloadScripts()
        {
            LuaRuntime.Instance.Reset();
            scriptLoader.ForceInit(scriptPath);
            flowChartGraph = scriptLoader.GetFlowChartGraph();
        }

        #region States

        /// <summary>
        /// The current node record in the global save file
        /// </summary>
        private NodeRecord nodeRecord;

        private long checkpointOffset;

        /// <summary>
        /// The current flow chart node
        /// </summary>
        public FlowChartNode currentNode { get; private set; }

        /// <summary>
        /// The index of the current dialogue entry in the current node
        /// </summary>
        private int currentIndex;

        /// <summary>
        /// The current dialogueEntry
        /// </summary>
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

        private State state = State.Ended;

        public bool isEnded => state == State.Ended;

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
            state = State.Ended;

            // Restore scene
            if (initialCheckpoint != null)
            {
                RestoreCheckpoint(initialCheckpoint);
            }
        }

        #endregion

        #region Events

        public UnityEvent gameStarted;

        /// <summary>
        /// This event will be triggered if the node has changed. The new node name will be sent to all listeners.
        /// </summary>
        public NodeChangedEvent nodeChanged;

        /// <summary>
        /// This event will be triggered if the content of the dialogue will change. It will be triggered before
        /// the lazy execution block of the new dialogue is invoked.
        /// </summary>
        public UnityEvent dialogueWillChange;

        /// <summary>
        /// This event will be triggered if the content of the dialogue has changed. The new dialogue text will be
        /// sent to all listeners.
        /// </summary>
        public DialogueChangedEvent dialogueChangedEarly;

        public DialogueChangedEvent dialogueChanged;

        /// <summary>
        /// This event will be triggered if a selection occurs, either when branches occur or when a selection is
        /// triggered from the script.
        /// </summary>
        public SelectionOccursEvent selectionOccurs;

        /// <summary>
        /// This event will be triggered if the story route has reached an end.
        /// </summary>
        public RouteEndedEvent routeEnded;

        public UnityEvent restoreStarts;

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
                    checkpointEnsured = true; // Always get a checkpoint at the beginning of the node
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
            dialogueWillChange.Invoke();

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
                var node = GetNode(pendingJumpTarget);
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
                stepsFromLastCheckpoint++;
            }

            if (atEndOfNodeRecord || (shouldSaveCheckpoint && currentIndex >= nodeRecord.endDialogue &&
                                      !checkpointManager.CanAppendCheckpoint(checkpointOffset)))
            {
                AppendSameNode();
            }

            var isReached = currentIndex < nodeRecord.endDialogue;
            if (shouldSaveCheckpoint)
            {
                StepCheckpoint(isReached);
            }

            if (!isReached)
            {
                checkpointManager.AppendDialogue(nodeRecord, currentIndex, shouldSaveCheckpoint);
            }

            if (shouldSaveCheckpoint)
            {
                stepsFromLastCheckpoint = 0;
            }

            if (checkpointRestrained)
            {
                stepsCheckpointRestrained--;
            }

            // As the action for this dialogue will be re-run, it's fine to just reset checkpointEnsured to false
            checkpointEnsured = false;

            return isReached;
        }

        private bool DialogueSaveReachedData(out ReachedDialogueData dialogueData)
        {
            var isReachedAnyHistory = checkpointManager.IsReachedAnyHistory(currentNode.name, currentIndex);

            if (!isReachedAnyHistory)
            {
                var voices = currentVoices.Count > 0 ? new Dictionary<string, VoiceEntry>(currentVoices) : null;
                dialogueData = new ReachedDialogueData(currentNode.name, currentIndex, voices,
                    currentDialogueEntry.NeedInterpolate(), currentDialogueEntry.textHash);
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
                    var endName = flowChartGraph.GetEndName(currentNode);
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
            checkpointEnsured = true;
        }

        private void MoveToNextNode(FlowChartNode nextNode)
        {
            scriptLoader.AddDeferredDialogueChunks(nextNode);
            // in case of empty node, do not change any of these
            // so the bookmark is left at the end of last node
            if (nextNode.dialogueEntryCount > 0)
            {
                nodeRecord = checkpointManager.GetNextNode(nodeRecord, nextNode.name, variables, 0);
                currentIndex = 0;
                checkpointOffset = nodeRecord.offset;
            }

            currentNode = nextNode;
            UpdateGameState(true, true, true, true, false);
        }

        private IEnumerator DoBranch(IEnumerable<BranchInformation> branchInfos)
        {
            var selections = new List<SelectionOccursData.Selection>();
            var selectionNames = new List<string>();
            foreach (var branchInfo in branchInfos)
            {
                if (branchInfo.mode == BranchMode.Jump)
                {
                    if (branchInfo.condition == null || branchInfo.condition.Invoke<bool>())
                    {
                        SelectBranch(branchInfo.name);
                        yield break;
                    }

                    continue;
                }

                if (branchInfo.mode == BranchMode.Show && !branchInfo.condition.Invoke<bool>())
                {
                    continue;
                }

                var selection = new SelectionOccursData.Selection(branchInfo.texts, branchInfo.imageInfo,
                    interactable: branchInfo.mode != BranchMode.Enable || branchInfo.condition.Invoke<bool>());
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

        // Called in Start after all restorables are initialized
        private void SaveInitialCheckpoint()
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
            state = State.Normal;
            gameStarted.Invoke();
            MoveToNextNode(startNode);
        }

        public void GameStart(string startName)
        {
            GameStart(flowChartGraph.GetNode(startName));
        }

        public FlowChartNode GetNode(string name, bool addDeferred = false)
        {
            var node = flowChartGraph.GetNode(name);
            if (addDeferred)
            {
                scriptLoader.AddDeferredDialogueChunks(node);
            }

            return node;
        }

        public IEnumerable<string> GetStartNodeNames(StartNodeType type = StartNodeType.Normal)
        {
            return flowChartGraph.GetStartNodeNames(type);
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
        /// <param name="restorable">The restorable object to be added</param>
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
        /// <param name="restorable">The restorable object to be removed</param>
        public void RemoveRestorable(IRestorable restorable)
        {
            restorables.Remove(restorable.restorableName);
        }

        /// <summary>
        /// Not all states of objects can be easily restored, like holding animations.
        /// We store some checkpoints, and other states can be restored by re-executing from the last checkpoint.
        /// At least one checkpoint will be saved every maxStepsFromLastCheckpoint, except during holding animations.
        /// </summary>
        [SerializeField] private int maxStepsFromLastCheckpoint = 10;

        public const int WarningStepsFromLastCheckpoint = 100;

        private int stepsFromLastCheckpoint;

        /// <summary>
        /// Restrain saving checkpoints.
        /// </summary>
        /// <remarks>
        /// This feature is necessary for restoring holding animations.
        /// This restraint has lower priority than checkpointEnsured.
        /// </remarks>
        private int stepsCheckpointRestrained;

        private bool checkpointRestrained => stepsCheckpointRestrained > 0;

        /// <summary>
        /// Restrain saving checkpoints for given steps.
        /// </summary>
        /// <param name="steps">number of steps to restrain checkpoints</param>
        /// <param name="overridden">if the new step number should overwrite the old one</param>
        public void RestrainCheckpoint(int steps, bool overridden = false)
        {
            if (!overridden && stepsCheckpointRestrained >= steps) return;
            if (steps < 0) steps = 0;
            stepsCheckpointRestrained = steps;
        }

        /// <summary>
        /// Used to force save a checkpoint before a holding animation begins.
        /// </summary>
        private bool checkpointEnsured;

        // Used by the preload system when using anim_hold_begin
        // Whether the current dialogue has a checkpoint is decided before the Lua code runs,
        // so we can only ensure it on the next dialogue
        public void EnsureCheckpointOnNextDialogue()
        {
            checkpointEnsured = true;
        }

        private bool atEndOfNodeRecord =>
            !isUpgrading && nodeRecord.child != 0 && currentIndex >= nodeRecord.endDialogue;

        private bool shouldSaveCheckpoint =>
            checkpointEnsured || atEndOfNodeRecord ||
            (!checkpointRestrained && stepsFromLastCheckpoint >= maxStepsFromLastCheckpoint);

        /// <summary>
        /// Get the current game state as a checkpoint
        /// </summary>
        private GameStateCheckpoint GetCheckpoint()
        {
            var restoreDatas = new Dictionary<string, IRestoreData>();
            foreach (var restorable in restorables)
            {
                restoreDatas[restorable.Key] = restorable.Value.GetRestoreData();
            }

            return new GameStateCheckpoint(currentIndex, restoreDatas, variables, stepsCheckpointRestrained);
        }

        private void RestoreCheckpoint(GameStateCheckpoint entry)
        {
            this.RuntimeAssert(entry != null, "Checkpoint is null.");

            currentIndex = entry.dialogueIndex;
            stepsFromLastCheckpoint = 0;
            stepsCheckpointRestrained = entry.stepsCheckpointRestrained;
            checkpointEnsured = false;

            variables.CloneFrom(entry.variables);

            var pairs = restorables.OrderByDescending(x =>
                (x.Value as IPrioritizedRestorable)?.priority ?? RestorablePriority.Normal);
            foreach (var pair in pairs)
            {
                if (entry.restoreDatas.TryGetValue(pair.Key, out var data))
                {
                    pair.Value.Restore(data);
                }
                else
                {
                    if (!isUpgrading)
                    {
                        Debug.LogWarning($"Nova: Key {pair.Key} not found in restoreDatas. Please clear save data.");
                    }

                    // fallback to initialCheckpoint state
                    pair.Value.Restore(initialCheckpoint.restoreDatas[pair.Key]);
                }
            }
        }

        private void SeekBackStep(int steps, IList<NodeRecord> nodeHistory, out long newCheckpointOffset,
            out int newDialogueIndex)
        {
            // Debug.Log($"stepback={steps}");
            this.RuntimeAssert(steps >= 0, $"Invalid steps {steps}.");

            nodeHistory.Clear();
            var hasLimit = steps > 0;

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

            if (!hasLimit || steps > endDialogue - curNode.beginDialogue)
            {
                newDialogueIndex = curNode.beginDialogue;
            }
            else
            {
                newDialogueIndex = endDialogue - steps;
            }

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

        public void SeekBackStep(int steps, out NodeRecord nodeRecord, out long newCheckpointOffset,
            out int newDialogueIndex)
        {
            var list = new List<NodeRecord>();
            SeekBackStep(steps, list, out newCheckpointOffset, out newDialogueIndex);
            nodeRecord = list[list.Count - 1];
        }

        public bool isUpgrading { get; private set; }

        public bool isRestoring { get; private set; }

        private bool CheckUnlockInRestoring()
        {
            if (!actionPauseLock.isLocked)
            {
                return true;
            }

            if (isUpgrading)
            {
                throw CheckpointCorruptedException.CannotUpgrade;
            }

            Debug.LogWarning("Nova: GameState paused by action when restoring. " +
                             "Maybe a minigame does not have a checkpoint ensured after it.");
            isRestoring = false;
            return false;
        }

        private void FastForward(int stepCount)
        {
            this.RuntimeAssert(stepCount > 0, $"Invalid stepCount {stepCount}.");

            for (var i = 0; i < stepCount; ++i)
            {
                if (!isUpgrading && i == stepCount - 1)
                {
                    isRestoring = false;
                }

                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                Step();
                if (!CheckUnlockInRestoring())
                {
                    return;
                }
            }
        }

        private void Move(NodeRecord newNodeRecord, long newCheckpointOffset, int dialogueIndex, bool upgrade)
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

            currentNode = GetNode(nodeRecord.name, true);
            // Debug.Log($"checkpoint={checkpointOffset} node={currentNode.name} dialogue={dialogueIndex} nodeDialogues={currentNode.dialogueEntryCount}");

            isRestoring = true;
            isUpgrading = upgrade;
            restoreStarts.Invoke();
            var checkpoint = checkpointManager.GetCheckpoint(checkpointOffset);
            RestoreCheckpoint(checkpoint);
            this.RuntimeAssert(dialogueIndex >= currentIndex,
                $"dialogueIndex {dialogueIndex} is before currentIndex {currentIndex}.");
            if (dialogueIndex == currentIndex)
            {
                isRestoring = false;
            }

            UpdateGameState(true, true, false, false, true);
            if (!CheckUnlockInRestoring())
            {
                return;
            }

            if (dialogueIndex > currentIndex)
            {
                FastForward(dialogueIndex - currentIndex);
            }

            isUpgrading = false;
            // Debug.Log($"MoveBackTo end {nodeHistoryEntry.Key} {nodeHistoryEntry.Value} {dialogueIndex}");
        }

        public void MoveBackTo(NodeRecord newNodeRecord, long newCheckpointOffset, int dialogueIndex)
        {
            Move(newNodeRecord, newCheckpointOffset, dialogueIndex, false);
        }

        public void MoveToUpgrade(NodeRecord newNodeRecord, int lastDialogue)
        {
            state = State.Normal;
            Move(newNodeRecord, checkpointManager.NextRecord(newNodeRecord.offset), lastDialogue, true);
            ResetGameState();
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
        /// Get the Bookmark for the current game state
        /// </summary>
        public Bookmark GetBookmark()
        {
            if (nodeRecord == null || checkpointOffset == nodeRecord.offset)
            {
                throw new InvalidOperationException("Nova: Cannot save bookmark at this point.");
            }

            return new Bookmark(nodeRecord, checkpointOffset, currentIndex);
        }

        /// <summary>
        /// Load a Bookmark and restore the saved game state
        /// </summary>
        public void LoadBookmark(Bookmark bookmark)
        {
            state = State.Normal;
            MoveBackTo(checkpointManager.GetNodeRecord(bookmark.nodeOffset), bookmark.checkpointOffset,
                bookmark.dialogueIndex);
        }

        #endregion

        // private string debugState => $"{currentNode?.name} {currentIndex} {variables.hash} | {stepsFromLastCheckpoint} {stepsCheckpointRestrained} {checkpointEnsured} {shouldSaveCheckpoint}";
    }
}
