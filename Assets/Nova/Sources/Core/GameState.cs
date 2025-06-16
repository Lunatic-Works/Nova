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

        private CoroutineHelper coroutineHelper;

        #region Init

        private void Awake()
        {
            coroutineHelper = new CoroutineHelper(this);

            LuaRuntime.Instance.BindObject("variables", variables);
            LuaRuntime.Instance.BindObject("coroutineHelper", coroutineHelper);

            try
            {
                scriptLoader.Init(scriptPath);
                flowChartGraph = scriptLoader.GetFlowChartGraph();

                checkpointManager = GetComponent<CheckpointManager>();
                checkpointManager.Init();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Utils.Quit();
            }
        }

        private void Start()
        {
            if (initialCheckpoint == null)
            {
                initialCheckpoint = GetCheckpoint();
            }

            CheckScriptUpgrade(false);
        }

        // It can run any Lua code, so everything that can be used in Lua code should initialize before Start
        private void CheckScriptUpgrade(bool updatePosition)
        {
            Bookmark curPosition = null;
            if (updatePosition)
            {
                curPosition = new Bookmark(nodeRecord.offset, currentIndex);
            }

            var upgradeStarted = false;
            var success = false;
            try
            {
                // Update globalSave.nodeHashes, flush and back up global save file
                var needUpgrade = checkpointManager.CheckScriptUpgrade(flowChartGraph, out var changedNodes);
                if (!needUpgrade)
                {
                    return;
                }

                upgradeStarted = true;
                var upgrader = new CheckpointUpgrader(this, checkpointManager, changedNodes);
                // UpgradeSaves may reset nodeRecord and currentIndex
                upgrader.UpgradeSaves();
                success = true;

                if (updatePosition)
                {
                    if (upgrader.TryUpgradeBookmark(curPosition))
                    {
                        LoadBookmark(curPosition);
                    }
                    else
                    {
                        Debug.LogError("Nova: Cannot reload script because the current node is deleted.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Nova: Script upgrade failed: {e}");
                if (upgradeStarted)
                {
                    checkpointManager.RestoreGlobalSave();
                }

                if (updatePosition && !success)
                {
                    LoadBookmark(curPosition);
                }
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
            CheckScriptUpgrade(true);
            Debug.Log($"Nova: Reload complete {nodeRecord} {currentIndex}");
        }

        #endregion

        #region Start node

        private void GameStart(FlowChartNode startNode)
        {
            ResetGameState();
            state = State.Normal;
            gameStarted.Invoke();
            MoveToNextNode(startNode);
        }

        public void GameStart(string nodeName)
        {
            GameStart(GetNode(nodeName));
        }

        public FlowChartNode GetNode(string name, bool addDeferred = true)
        {
            var node = flowChartGraph.GetNode(name);
            if (addDeferred)
            {
                ScriptLoader.AddDeferredDialogueChunks(node);
            }

            return node;
        }

        public IEnumerable<string> GetStartNodeNames(StartNodeType type = StartNodeType.Normal)
        {
            return flowChartGraph.GetStartNodeNames(type);
        }

        #endregion

        #region States

        /// <summary>
        /// Represents the current node history
        /// </summary>
        private NodeRecord nodeRecord;

        public FlowChartNode currentNode => nodeRecord == null ? null : GetNode(nodeRecord.name);

        public int currentIndex { get; private set; }

        private DialogueEntry currentDialogueEntry
        {
            get
            {
                var node = currentNode;
                if (node == null)
                {
                    return null;
                }

                if (node.dialogueEntryCount == 0)
                {
                    Debug.LogWarning($"Nova: currentNode should be non-empty: {node.name}");
                    return null;
                }

                return node.GetDialogueEntryAt(currentIndex);
            }
        }

        public DialogueDisplayData currentDialogueDisplayData => currentDialogueEntry?.GetDisplayData();

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
            currentIndex = 0;
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
        /// Triggered after the node changes.
        /// </summary>
        public NodeChangedEvent nodeChanged;

        /// <summary>
        /// Triggered after the checkpoint is saved, before the default lazy execution block is invoked.
        /// </summary>
        public UnityEvent dialogueWillChange;

        /// <summary>
        /// Triggered after the default lazy execution block is invoked.
        /// </summary>
        public DialogueChangedEvent dialogueChangedEarly;

        public DialogueChangedEvent dialogueChanged;

        /// <summary>
        /// Triggered when choices occur, either when branches occur or when choices are raised from the script.
        /// </summary>
        public ChoiceOccursEvent choiceOccurs;

        /// <summary>
        /// Triggered before a node which is a save point reaches its end.
        /// </summary>
        public SavePointEvent savePoint;

        /// <summary>
        /// Triggered when the story route reaches an end.
        /// </summary>
        public RouteEndedEvent routeEnded;

        public UnityEvent<bool> restoreStarts;

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
                appendNodeEnsured = true;
            }
        }

        public void SignalFence(object value)
        {
            coroutineHelper.SignalFence(value);
        }

        #endregion

        #region Step forward

        /// <summary>
        /// Called after the current node or the current dialogue index changes
        /// </summary>
        /// <remarks>
        /// Trigger events according to the current states and how they were changed
        /// Assume currentNode is non-empty
        /// </remarks>
        private void UpdateGameState(bool fromCheckpoint, bool nodeChanged)
        {
            // Debug.Log($"UpdateGameState begin {debugState}");

            if (nodeChanged)
            {
                // Debug.Log($"Node changed to {nodeRecord}");

                this.nodeChanged.Invoke(new NodeChangedData(nodeRecord.name));
                // Always get a checkpoint at the beginning of the node
                checkpointEnsured = true;
            }

            this.RuntimeAssert(currentIndex >= 0 && currentIndex < currentNode.dialogueEntryCount,
                               $"Dialogue index {currentIndex} out of range [0, {currentNode.dialogueEntryCount})");
            ExecuteAction(UpdateDialogue(fromCheckpoint, nodeChanged));

            // Debug.Log($"UpdateGameState end {debugState}");
        }

        private IEnumerator UpdateDialogue(bool fromCheckpoint, bool nodeChanged)
        {
            if (!fromCheckpoint)
            {
                // If the following lines of code are put into a new coroutine, one frame's delay will be introduced,
                // so don't do that
                currentDialogueEntry.ExecuteAction(DialogueActionStage.BeforeCheckpoint, isRestoring);
                while (actionPauseLock.isLocked) yield return null;
            }

            var isReached = DialogueSaveCheckpoint(nodeChanged);
            dialogueWillChange.Invoke();

            currentDialogueEntry.ExecuteAction(DialogueActionStage.Default, isRestoring);
            while (actionPauseLock.isLocked) yield return null;

            var isReachedAnyHistory = checkpointManager.IsReachedAnyHistory(nodeRecord.name, currentIndex);
            var dialogueData = DialogueSaveReachedData(isReachedAnyHistory);
            var dialogueChangedData = new DialogueChangedData(nodeRecord, dialogueData,
                currentDialogueDisplayData, isReached, isReachedAnyHistory);
            if (isJumping && !isReachedAnyHistory)
            {
                isJumping = false;
            }

            dialogueChangedEarly.Invoke(dialogueChangedData);
            dialogueChanged.Invoke(dialogueChangedData);

            currentDialogueEntry.ExecuteAction(DialogueActionStage.AfterDialogue, isRestoring);
            while (actionPauseLock.isLocked) yield return null;
        }

        private bool DialogueSaveCheckpoint(bool nodeChanged)
        {
            if (!nodeChanged)
            {
                stepsFromLastCheckpoint++;
            }

            var atEndOfNodeRecord = !isUpgrading && nodeRecord.child != 0 && currentIndex >= nodeRecord.endDialogue;
            var isReached = currentIndex < nodeRecord.endDialogue;
            if (appendNodeEnsured ||
                atEndOfNodeRecord ||
                (shouldSaveCheckpoint && !isReached && !checkpointManager.IsLastNodeRecord(nodeRecord)))
            {
                AppendSameNode();
                checkpointEnsured = true;
                appendNodeEnsured = false;
                isReached = currentIndex < nodeRecord.endDialogue;
            }

            if (shouldSaveCheckpoint && !isReached)
            {
                // Debug.Log($"AppendCheckpoint {nodeRecord.name} @{checkpointManager.endCheckpoint} {currentIndex}");
                checkpointManager.AppendCheckpoint(currentIndex, GetCheckpoint());
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

        private void AppendSameNode()
        {
            // var oldNodeRecord = nodeRecord;
            nodeRecord = checkpointManager.GetNextNodeRecord(nodeRecord, nodeRecord.name, variables, currentIndex);
            // Debug.Log($"AppendSameNode {oldNodeRecord} -> {nodeRecord}");
        }

        private ReachedDialogueData DialogueSaveReachedData(bool isReachedAnyHistory)
        {
            ReachedDialogueData dialogueData;
            if (!isReachedAnyHistory)
            {
                var voices = currentVoices.Count > 0 ? new Dictionary<string, VoiceEntry>(currentVoices) : null;
                dialogueData = new ReachedDialogueData(nodeRecord.name, currentIndex, voices,
                    currentDialogueEntry.NeedInterpolate(), currentDialogueEntry.textHash);
                checkpointManager.SetReachedDialogue(dialogueData);
            }
            else
            {
                dialogueData = checkpointManager.GetReachedDialogue(nodeRecord.name, currentIndex);
            }

            return dialogueData;
        }

        private void StepAtEndOfNode(FlowChartNode node)
        {
            if (node.isSavePoint)
            {
                this.RuntimeAssert(node.dialogueEntryCount > 0, $"Empty node cannot be save point: {node.name}");
                savePoint.Invoke(new SavePointEventData());
            }

            switch (node.type)
            {
                case FlowChartNodeType.Normal:
                    MoveToNextNode(node.next);
                    break;
                case FlowChartNodeType.Branching:
                    ExecuteAction(DoBranch(node));
                    break;
                case FlowChartNodeType.End:
                    state = State.Ended;
                    checkpointManager.SetReachedEnd(node.name);
                    routeEnded.Invoke(new RouteEndedData(node.name));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MoveToNextNode(FlowChartNode node)
        {
            ScriptLoader.AddDeferredDialogueChunks(node);
            if (node.dialogueEntryCount > 0)
            {
                nodeRecord = checkpointManager.GetNextNodeRecord(nodeRecord, node.name, variables, 0);
                currentIndex = 0;
                UpdateGameState(false, true);
            }
            else
            {
                // When moving to an empty node, MoveToNextNode and StepAtEndOfNode may be called multiple times,
                // without changing nodeRecord, currentNode, and currentIndex
                // We assume nodeRecord and currentNode are always non-empty
                StepAtEndOfNode(node);
            }
        }

        private IEnumerator DoBranch(FlowChartNode node)
        {
            var choices = new List<ChoiceOccursData.Choice>();
            var choiceNames = new List<string>();
            foreach (var branchInfo in node.GetAllBranches())
            {
                if (branchInfo.mode == BranchMode.Jump)
                {
                    if (branchInfo.condition == null || branchInfo.condition.Invoke<bool>())
                    {
                        SelectBranch(node, branchInfo.name);
                        yield break;
                    }

                    continue;
                }

                if (branchInfo.mode == BranchMode.Show && !branchInfo.condition.Invoke<bool>())
                {
                    continue;
                }

                var choice = new ChoiceOccursData.Choice(branchInfo.texts, branchInfo.imageInfo,
                    interactable: branchInfo.mode != BranchMode.Enable || branchInfo.condition.Invoke<bool>());
                choices.Add(choice);
                choiceNames.Add(branchInfo.name);
            }

            AcquireActionPause();

            RaiseChoices(choices);
            while (coroutineHelper.fence == null)
            {
                yield return null;
            }

            ReleaseActionPause();

            var index = (int)coroutineHelper.TakeFence();
            SelectBranch(node, choiceNames[index]);
        }

        private void SelectBranch(FlowChartNode node, string branchName)
        {
            MoveToNextNode(node.GetNext(branchName));
        }

        /// <summary>
        /// Check if the current state can step forward, i.e., something will happen when calling Step
        /// </summary>
        public bool canStepForward
        {
            get
            {
                if (currentNode == null)
                {
                    Debug.LogError("Nova: Cannot step forward before the game starts.");
                    return false;
                }

                if (isEnded)
                {
                    Debug.LogError("Nova: Cannot step when the game is ended.");
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
                UpdateGameState(false, false);
            }
            else
            {
                StepAtEndOfNode(currentNode);
            }
        }

        public void RaiseChoices(IReadOnlyList<ChoiceOccursData.Choice> choices)
        {
            Utils.RuntimeAssert(choices.Count > 0, "Nova: Choices must not be empty.");
            choiceOccurs.Invoke(new ChoiceOccursData(choices));
        }

        #endregion

        #region Restore checkpoint

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

        // ensure a new nodeRecord + checkpoint at next UpdateDialogue
        private bool appendNodeEnsured;

        private bool shouldSaveCheckpoint =>
            checkpointEnsured ||
            (!checkpointRestrained && stepsFromLastCheckpoint >= maxStepsFromLastCheckpoint);

        /// <summary>
        /// Get the current game state as a checkpoint
        /// </summary>
        public Dictionary<string, IRestoreData> GetRestorableDatas()
        {
            return restorables.ToDictionary(x => x.Key, x => x.Value.GetRestoreData());
            }

        private GameStateCheckpoint GetCheckpoint()
        {
            var restoreDatas = GetRestorableDatas();
            return new GameStateCheckpoint(currentIndex, restoreDatas, variables, stepsCheckpointRestrained);
        }

        // Does not update nodeRecord, updates currentIndex
        private void RestoreCheckpoint(GameStateCheckpoint entry)
        {
            this.RuntimeAssert(entry != null, "Checkpoint is null.");
            restoreStarts.Invoke(entry == initialCheckpoint);

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
                    // fallback to initialCheckpoint state
                    pair.Value.Restore(initialCheckpoint.restoreDatas[pair.Key]);
                }
            }
        }

        #endregion

        #region Combinations of RestoreCheckpoint + Step

        private int SeekBackStep(int steps, IList<NodeRecord> nodeHistory, out int newDialogueIndex)
        {
            // Debug.Log($"SeekBackStep {steps}");

            this.RuntimeAssert(steps >= 0, $"Invalid steps {steps}.");

            nodeHistory.Clear();
            var hasLimit = steps > 0;
            var totSteps = 0;

            var curNode = nodeRecord;
            nodeHistory.Add(curNode);
            var endDialogue = currentIndex;
            while (curNode.parent != 0 && (!hasLimit || steps > endDialogue - curNode.beginDialogue))
            {
                totSteps += endDialogue - curNode.beginDialogue;
                if (hasLimit)
                {
                    steps -= totSteps;
                }

                curNode = checkpointManager.GetNodeRecord(curNode.parent);
                nodeHistory.Add(curNode);
                endDialogue = curNode.endDialogue;
            }

            if (!hasLimit || steps > endDialogue - curNode.beginDialogue)
            {
                totSteps += endDialogue - curNode.beginDialogue;
                newDialogueIndex = curNode.beginDialogue;
            }
            else
            {
                totSteps += steps;
                newDialogueIndex = endDialogue - steps;
            }

            // Debug.Log($"newNode @{nodeHistory[0].offset} newDialogueIndex {newDialogueIndex}");
            return totSteps;
        }

        public bool isRestoring { get; private set; }
        public bool isUpgrading { get; private set; }
        public bool isJumping { get; private set; }

        private bool CheckUnlockInRestoring()
        {
            if (!actionPauseLock.isLocked)
            {
                return true;
            }

            isRestoring = false;

            if (isUpgrading)
            {
                isUpgrading = false;
                throw CheckpointCorruptedException.CannotUpgrade;
            }

            Debug.LogWarning("Nova: GameState paused by action when restoring. " +
                             "Maybe a minigame does not have a checkpoint ensured after it.");
            return false;
        }

        private void JumpForward(int stepCount, bool fromMove)
        {
            this.RuntimeAssert(stepCount > 0, $"Invalid stepCount {stepCount}.");

            for (var i = 0; i < stepCount; ++i)
            {
                if (!isUpgrading && !isJumping && i == stepCount - 1)
                {
                    isRestoring = false;
                }

                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                Step();
                if (isUpgrading && i == stepCount - 1)
                {
                    NovaAnimation.StopAll(AnimationType.All ^ AnimationType.UI);
                }

                if (!fromMove && !isJumping)
                {
                    return;
                }

                if (!CheckUnlockInRestoring())
                {
                    return;
                }
            }
        }

        // If dialogueIndex >= newNodeRecord.endDialogue, then move to or create a new nodeRecord
        public void Move(NodeRecord newNodeRecord, int dialogueIndex)
        {
            // Debug.Log($"Move begin {nodeRecord} {currentIndex} -> {newNodeRecord} {dialogueIndex}");

            this.RuntimeAssert(dialogueIndex >= newNodeRecord.beginDialogue,
                $"dialogueIndex {dialogueIndex} < beginDialogue {newNodeRecord.beginDialogue}");

            CancelAction();
            NovaAnimation.StopAll(AnimationType.All ^ AnimationType.UI);
            LuaRuntime.Instance.GetFunction("action_before_move").Call();

            nodeRecord = newNodeRecord;

            // Find the last checkpoint before or at dialogueIndex
            var checkpointOffset = checkpointManager.NextRecord(nodeRecord.offset);
            var checkpointDialogueIndex = checkpointManager.GetCheckpointDialogueIndex(checkpointOffset);
            while (true)
            {
                if (checkpointDialogueIndex >= nodeRecord.lastCheckpointDialogue)
                {
                    break;
                }

                var nextCheckpointOffset = checkpointManager.NextCheckpoint(checkpointOffset);
                var nextCheckpointDialogueIndex = checkpointManager.GetCheckpointDialogueIndex(nextCheckpointOffset);
                if (nextCheckpointDialogueIndex > dialogueIndex)
                {
                    break;
                }

                checkpointOffset = nextCheckpointOffset;
                checkpointDialogueIndex = nextCheckpointDialogueIndex;
            }

            // Debug.Log($"checkpoint @{checkpointOffset} {checkpointDialogueIndex} {currentIndex} {dialogueIndex}");

            var checkpoint = checkpointManager.GetCheckpoint(checkpointOffset);
            isRestoring = true;
            RestoreCheckpoint(checkpoint);
            // Now currentIndex <= dialogueIndex
            if (!isUpgrading && dialogueIndex == currentIndex)
            {
                isRestoring = false;
            }

            // The result of invoking nodeChanged should be already in the checkpoint, so we don't invoke it here
            UpdateGameState(true, false);
            if (isUpgrading && dialogueIndex == currentIndex)
            {
                NovaAnimation.StopAll(AnimationType.All ^ AnimationType.UI);
            }

            if (!CheckUnlockInRestoring())
            {
                return;
            }

            if (dialogueIndex > currentIndex)
            {
                JumpForward(dialogueIndex - currentIndex, true);
            }

            isRestoring = false;

            // Debug.Log($"Move end {nodeRecord} {currentIndex} -> {newNodeRecord} {dialogueIndex}");
        }

        public void MoveUpgrade(NodeRecord newNodeRecord, int lastDialogue)
        {
            state = State.Normal;
            isUpgrading = true;
            Move(newNodeRecord, lastDialogue);
            isUpgrading = false;
            ResetGameState();
        }

        public void StepBackward()
        {
            int newDialogueIndex;
            NodeRecord newNodeRecord;
            var i = 1;
            while (true)
            {
                var list = new List<NodeRecord>();
                var step = SeekBackStep(i, list, out newDialogueIndex);
                newNodeRecord = list[list.Count - 1];
                if (step < i)
                {
                    break;
                }

                var newNode = GetNode(newNodeRecord.name);
                var dialogueEntry = newNode.GetDialogueEntryAt(newDialogueIndex);
                if (!dialogueEntry.IsEmpty())
                {
                    break;
                }

                i++;
            }

            Move(newNodeRecord, newDialogueIndex);
        }

        /// <summary>
        /// Move to previous/next chapter/branch.
        /// </summary>
        /// <param name="forward">Moving forward or backward.</param>
        /// <param name="allowChapter">Whether to stop at chapter.</param>
        /// <param name="allowBranch">Whether to stop at branch, only works in backward mode.</param>
        /// <returns>Whether succeeded. if not will move to the beginning/end</returns>
        public void MoveToKeyPoint(bool forward, bool allowChapter, bool allowBranch = true)
        {
            var entryNode = nodeRecord;
            var foundHead = false;
            if (forward)
            {
                allowBranch = true;
            }

            while (true)
            {
                var node = GetNode(entryNode.name);
                // we move to either node start (chapter) or end (branch)
                // stored in foundHead
                //
                // special handling of current node
                // 1. if going backward, the current node branch should not be considered
                // 2. if going forward, or right at the beginning, the current node beginning should not be considered
                var isBranch = allowBranch &&
                               entryNode.endDialogue == node.dialogueEntryCount &&
                               node.IsManualBranchNode() &&
                               (!(entryNode == nodeRecord && !forward));
                var isChapter = allowChapter &&
                                entryNode.beginDialogue == 0 &&
                                node.isChapter &&
                                (!(entryNode == nodeRecord && (currentIndex == 0 || forward)));
                if (isBranch || isChapter)
                {
                    foundHead = forward ? isChapter : !isBranch;
                    break;
                }

                var next = forward ? entryNode.child : entryNode.parent;
                if (next == 0)
                {
                    foundHead = !forward;
                    break;
                }

                var nextEntryNode = checkpointManager.GetNodeRecord(next);
                // multiple children case should only happen in interrupt (minigame)
                if (forward && nextEntryNode.sibling != 0)
                {
                    foundHead = false;
                    break;
                }

                entryNode = nextEntryNode;
            }

            var offset = checkpointManager.NextRecord(entryNode.offset);
            var dialogue = entryNode.beginDialogue;
            if (!foundHead)
            {
                dialogue = entryNode.endDialogue - 1;
                while (checkpointManager.GetCheckpointDialogueIndex(offset) != entryNode.lastCheckpointDialogue)
                {
                    offset = checkpointManager.NextCheckpoint(offset);
                }
            }

            Move(entryNode, dialogue);
        }

        public void JumpToNextChapter(bool allowChapter)
        {
            var jumped = false;
            while (true)
            {
                if (jumped && allowChapter && currentNode.isChapter)
                {
                    break;
                }

                isJumping = true;
                if (currentIndex < currentNode.dialogueEntryCount - 1)
                {
                    JumpForward(currentNode.dialogueEntryCount - currentIndex - 1, false);
                }

                if (!isJumping)
                {
                    Alert.Show("dialogue.noreadtext");
                    break;
                }

                isJumping = false;
                if (currentNode.type == FlowChartNodeType.End)
                {
                    break;
                }

                if (currentNode.IsManualBranchNode())
                {
                    if (!actionPauseLock.isLocked)
                    {
                        // Show choices
                        NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                        Step();
                    }

                    break;
                }

                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                Step();
                jumped = true;
            }
        }

        public IEnumerable<ReachedDialoguePosition> GetDialogueHistory(int limit)
        {
            if (nodeRecord == null)
            {
                yield break;
            }

            var nodeHistory = new List<NodeRecord>();
            SeekBackStep(limit, nodeHistory, out var curDialogue);

            for (var i = nodeHistory.Count - 1; i >= 0; --i)
            {
                var curNode = nodeHistory[i];
                if (i != nodeHistory.Count - 1)
                {
                    curDialogue = curNode.beginDialogue;
                }

                var endDialogue = i == 0 ? currentIndex : curNode.endDialogue;
                while (curDialogue < endDialogue)
                {
                    yield return new ReachedDialoguePosition(curNode, curDialogue);
                    ++curDialogue;
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
            if (nodeRecord == null)
            {
                throw new InvalidOperationException("Nova: Cannot save bookmark at this point.");
            }

            return new Bookmark(nodeRecord.offset, currentIndex);
        }

        /// <summary>
        /// Load a Bookmark and restore the saved game state
        /// </summary>
        public void LoadBookmark(Bookmark bookmark)
        {
            state = State.Normal;
            Move(checkpointManager.GetNodeRecord(bookmark.nodeOffset), bookmark.dialogueIndex);
        }

        #endregion

        // private string debugState => $"{nodeRecord} {currentIndex} {variables.hash} | {stepsFromLastCheckpoint} {stepsCheckpointRestrained} {checkpointEnsured} {shouldSaveCheckpoint}";
    }
}
