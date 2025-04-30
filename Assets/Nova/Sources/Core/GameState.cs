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
                curPosition = new Bookmark(nodeRecord.offset, checkpointOffset, currentIndex);
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
                upgrader.UpgradeSaves();
                success = true;

                if (updatePosition)
                {
                    if (upgrader.UpgradeBookmark(curPosition))
                    {
                        LoadBookmark(curPosition);
                    }
                    else if (currentNode != null)
                    {
                        // if we cannot update the current position, we start as if enter from the beginning of the node
                        GameStart(currentNode.name);
                    }
                    else
                    {
                        Debug.LogError("Nova: Cannot reload script because the current node is deleted");
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
            currentNode = GetNode(nodeRecord.name);
            Debug.Log("Nova: Reload complete.");
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
        public int currentIndex { get; private set; }

        /// <summary>
        /// The current dialogueEntry
        /// </summary>
        private DialogueEntry currentDialogueEntry;

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
        /// Called after the current node or the current dialogue index has changed
        /// </summary>
        /// <remarks>
        /// Trigger events according to the current states and how they were changed
        /// </remarks>
        private void UpdateGameState(bool fromCheckpoint, bool nodeChanged)
        {
            // Debug.Log($"UpdateGameState begin {debugState}");

            if (nodeChanged)
            {
                // Debug.Log($"Node changed to {currentNode.name}");

                this.nodeChanged.Invoke(new NodeChangedData(nodeRecord.name));

                // Always get a checkpoint at the beginning of the node
                checkpointEnsured = true;
            }

            if (currentNode.dialogueEntryCount > 0)
            {
                this.RuntimeAssert(currentIndex >= 0 && currentIndex < currentNode.dialogueEntryCount,
                                   $"Dialogue index {currentIndex} out of range [0, {currentNode.dialogueEntryCount})");
                currentDialogueEntry = currentNode.GetDialogueEntryAt(currentIndex);
                ExecuteAction(UpdateDialogue(fromCheckpoint, nodeChanged));
            }
            else
            {
                StepAtEndOfNode();
            }

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

            var isReached = currentIndex < nodeRecord.endDialogue;
            DialogueSaveCheckpoint(nodeChanged, isReached);
            dialogueWillChange.Invoke();

            currentDialogueEntry.ExecuteAction(DialogueActionStage.Default, isRestoring);
            while (actionPauseLock.isLocked) yield return null;

            var isReachedAnyHistory = checkpointManager.IsReachedAnyHistory(currentNode.name, currentIndex);
            var dialogueData = DialogueSaveReachedData(isReachedAnyHistory);
            var dialogueChangedData = new DialogueChangedData(nodeRecord, checkpointOffset, dialogueData,
                currentDialogueEntry.GetDisplayData(), isReached, isReachedAnyHistory);
            if (isJumping && !isReachedAnyHistory)
            {
                isJumping = false;
            }

            dialogueChangedEarly.Invoke(dialogueChangedData);
            dialogueChanged.Invoke(dialogueChangedData);

            currentDialogueEntry.ExecuteAction(DialogueActionStage.AfterDialogue, isRestoring);
            while (actionPauseLock.isLocked) yield return null;
        }

        private void DialogueSaveCheckpoint(bool nodeChanged, bool isReached)
        {
            if (!nodeChanged)
            {
                stepsFromLastCheckpoint++;
            }

            if (atEndOfNodeRecord || appendNodeEnsured ||
                (shouldSaveCheckpoint && currentIndex >= nodeRecord.endDialogue &&
                 !checkpointManager.CanAppendCheckpoint(checkpointOffset)))
            {
                AppendSameNode();
            }

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
        }

        private void AppendSameNode()
        {
            nodeRecord = checkpointManager.GetNextNodeRecord(nodeRecord, nodeRecord.name, variables, currentIndex);
            checkpointOffset = nodeRecord.offset;
            checkpointEnsured = true;
            appendNodeEnsured = false;
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

        private ReachedDialogueData DialogueSaveReachedData(bool isReachedAnyHistory)
        {
            ReachedDialogueData dialogueData;
            if (!isReachedAnyHistory)
            {
                var voices = currentVoices.Count > 0 ? new Dictionary<string, VoiceEntry>(currentVoices) : null;
                dialogueData = new ReachedDialogueData(currentNode.name, currentIndex, voices,
                    currentDialogueEntry.NeedInterpolate(), currentDialogueEntry.textHash);
                checkpointManager.SetReachedDialogue(dialogueData);
            }
            else
            {
                dialogueData = checkpointManager.GetReachedDialogue(currentNode.name, currentIndex);
            }

            return dialogueData;
        }

        private void StepAtEndOfNode()
        {
            if (currentNode.isSavePoint)
            {
                savePoint.Invoke(new SavePointEventData());
            }

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
                    checkpointManager.SetReachedEnd(endName);
                    routeEnded.Invoke(new RouteEndedData(endName));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MoveToNextNode(FlowChartNode nextNode)
        {
            ScriptLoader.AddDeferredDialogueChunks(nextNode);
            // in case of empty node, do not change any of these
            // so the bookmark is left at the end of last node
            if (nextNode.dialogueEntryCount > 0)
            {
                nodeRecord = checkpointManager.GetNextNodeRecord(nodeRecord, nextNode.name, variables, 0);
                currentIndex = 0;
                checkpointOffset = nodeRecord.offset;
            }

            currentNode = nextNode;
            UpdateGameState(false, true);
        }

        private IEnumerator DoBranch(IEnumerable<BranchInformation> branchInfos)
        {
            var choices = new List<ChoiceOccursData.Choice>();
            var choiceNames = new List<string>();
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
            SelectBranch(choiceNames[index]);
        }

        private void SelectBranch(string branchName)
        {
            MoveToNextNode(currentNode.GetNext(branchName));
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
                StepAtEndOfNode();
            }
        }

        public void RaiseChoices(IReadOnlyList<ChoiceOccursData.Choice> choices)
        {
            Utils.RuntimeAssert(choices.Count > 0, "Nova: Choices must not be empty.");
            choiceOccurs.Invoke(new ChoiceOccursData(choices));
        }

        #endregion

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

        // ensure a new nodeRecord + checkpoint at next UpdateDialogue
        private bool appendNodeEnsured;

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

        private int SeekBackStep(int steps, IList<NodeRecord> nodeHistory, out long newCheckpointOffset,
            out int newDialogueIndex)
        {
            // Debug.Log($"stepback={steps}");
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

            newCheckpointOffset = checkpointManager.NextRecord(curNode.offset);
            var checkpointDialogue = checkpointManager.GetCheckpointDialogueIndex(newCheckpointOffset);
            while (checkpointDialogue < curNode.lastCheckpointDialogue)
            {
                var nextCheckpoint = checkpointManager.NextCheckpoint(newCheckpointOffset);
                var nextCheckpointDialogue = checkpointManager.GetCheckpointDialogueIndex(nextCheckpoint);
                if (nextCheckpointDialogue > newDialogueIndex)
                {
                    break;
                }

                newCheckpointOffset = nextCheckpoint;
                checkpointDialogue = nextCheckpointDialogue;
            }

            // Debug.Log($"newNode=@{nodeHistory[0].offset} newCheckpointOffset=@{newCheckpointOffset} newDialogueIndex={newDialogueIndex}");
            return totSteps;
        }

        public void MoveBackward()
        {
            long newCheckpointOffset;
            int newDialogueIndex;
            NodeRecord newNodeRecord;
            var i = 1;
            while (true)
            {
                var list = new List<NodeRecord>();
                var step = SeekBackStep(i, list, out newCheckpointOffset, out newDialogueIndex);
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

            MoveBackTo(newNodeRecord, newCheckpointOffset, newDialogueIndex);
        }

        public void SeekBackStep(int steps, out NodeRecord nodeRecord, out long newCheckpointOffset,
            out int newDialogueIndex)
        {
            var list = new List<NodeRecord>();
            SeekBackStep(steps, list, out newCheckpointOffset, out newDialogueIndex);
            nodeRecord = list[list.Count - 1];
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

        private void FastForward(int stepCount)
        {
            this.RuntimeAssert(stepCount > 0, $"Invalid stepCount {stepCount}.");

            var jumping = isJumping;
            for (var i = 0; i < stepCount; ++i)
            {
                if (!isUpgrading && !isJumping && i == stepCount - 1)
                {
                    isRestoring = false;
                }

                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                Step();
                if (jumping && !isJumping)
                {
                    return;
                }

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

            LuaRuntime.Instance.GetFunction("action_before_move").Call();

            // Restore history
            nodeRecord = newNodeRecord;
            checkpointOffset = newCheckpointOffset;
            if (checkpointOffset == nodeRecord.offset)
            {
                checkpointOffset = checkpointManager.NextRecord(checkpointOffset);
            }

            currentNode = GetNode(nodeRecord.name);
            // Debug.Log($"checkpoint={checkpointOffset} node={currentNode.name} dialogue={dialogueIndex} nodeDialogues={currentNode.dialogueEntryCount}");

            isRestoring = true;
            isUpgrading = upgrade;
            var checkpoint = checkpointManager.GetCheckpoint(checkpointOffset);
            RestoreCheckpoint(checkpoint);
            if (dialogueIndex == currentIndex)
            {
                isRestoring = false;
            }

            // The result of invoking nodeChanged should be already in the checkpoint, so we don't invoke it here
            UpdateGameState(true, false);
            if (!CheckUnlockInRestoring())
            {
                return;
            }

            if (dialogueIndex > currentIndex)
            {
                FastForward(dialogueIndex - currentIndex);
            }

            isRestoring = false;
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
            // Move does not stop animations in the last step
            NovaAnimation.StopAll(AnimationType.All ^ AnimationType.UI);
            ResetGameState();
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
                var isBranch = allowBranch && entryNode.endDialogue == node.dialogueEntryCount &&
                               node.IsManualBranchNode() && (!(entryNode == nodeRecord && !forward));
                var isChapter = allowChapter && entryNode.beginDialogue == 0 && node.isChapter &&
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

            MoveBackTo(entryNode, offset, dialogue);
        }

        public void JumpToNextChapter()
        {
            var jumped = false;
            while (true)
            {
                if (jumped && currentNode.isChapter)
                {
                    break;
                }

                isJumping = true;
                if (currentIndex < currentNode.dialogueEntryCount - 1)
                {
                    FastForward(currentNode.dialogueEntryCount - currentIndex - 1);
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

        public IEnumerable<ReachedDialoguePosition> GetDialogueHistory(int limit = 0)
        {
            if (nodeRecord == null)
            {
                yield break;
            }

            var nodeHistory = new List<NodeRecord>();
            SeekBackStep(limit, nodeHistory, out var curCheckpoint, out var curDialogue);

            for (var i = nodeHistory.Count - 1; i >= 0; i--)
            {
                var curNode = nodeHistory[i];
                if (i != nodeHistory.Count - 1)
                {
                    curCheckpoint = checkpointManager.NextRecord(curNode.offset);
                    curDialogue = curNode.beginDialogue;
                }

                var checkpointDialogue = checkpointManager.GetCheckpointDialogueIndex(curCheckpoint);
                var endDialogue = i == 0 ? currentIndex : curNode.endDialogue;
                while (curDialogue < endDialogue)
                {
                    var curEndDialogue = endDialogue;
                    long nextCheckpoint = 0;
                    if (checkpointDialogue < curNode.lastCheckpointDialogue)
                    {
                        nextCheckpoint = checkpointManager.NextCheckpoint(curCheckpoint);
                        var nextCheckpointDialogue = checkpointManager.GetCheckpointDialogueIndex(nextCheckpoint);
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

            return new Bookmark(nodeRecord.offset, checkpointOffset, currentIndex);
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
