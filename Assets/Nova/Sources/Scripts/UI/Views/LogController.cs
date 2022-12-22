using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nova
{
    public class LogEntry
    {
        public readonly float height;
        public float prefixHeight;
        public readonly long nodeOffset;
        public readonly long checkpointOffset;
        public readonly ReachedDialogueData dialogueData;
        public readonly DialogueDisplayData displayData;

        public LogEntry(float height, float prefixHeight, long nodeOffset, long checkpointOffset,
            ReachedDialogueData dialogueData, DialogueDisplayData displayData)
        {
            this.height = height;
            this.prefixHeight = prefixHeight;
            this.nodeOffset = nodeOffset;
            this.checkpointOffset = checkpointOffset;
            this.dialogueData = dialogueData;
            this.displayData = displayData;
        }
    }

    public class LogController : ViewControllerBase, IRestorable, LoopScrollPrefabSource, LoopScrollDataSource,
        LoopScrollSizeHelper
    {
        [Serializable]
        private class LogEntryRestoreData
        {
            public readonly DialogueDisplayData displayData;
            public int index;

            public LogEntryRestoreData(DialogueDisplayData displayData, int index)
            {
                this.displayData = displayData;
                this.index = index;
            }
        }

        [SerializeField] private int maxLogEntryNum = 1000;
        [SerializeField] private LogEntryController logEntryPrefab;
        [SerializeField] private Button closeButton;

        private const string LogViewFirstShownKey = ConfigManager.FirstShownKeyPrefix + "LogView";

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ConfigManager configManager;

        private LoopScrollRect scrollRect;

        private LogEntryController logEntryForTest;
        private TMP_Text contentForTest;
        private float contentDefaultWidth;

        private readonly List<LogEntry> logEntries = new List<LogEntry>();
        private readonly List<LogEntryRestoreData> logEntriesRestoreData = new List<LogEntryRestoreData>();

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            configManager = controller.ConfigManager;

            scrollRect = myPanel.GetComponentInChildren<LoopScrollRect>();
            scrollRect.prefabSource = this;
            scrollRect.dataSource = this;
            scrollRect.sizeHelper = this;

            myPanel.GetComponent<Button>().onClick.AddListener(Hide);
            closeButton.onClick.AddListener(Hide);

            gameState.gameStarted.AddListener(Clear);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            gameState.AddRestorable(this);
        }

        protected override void ForceRebuildLayoutAndResetTransitionTarget()
        {
            // fake content to test size
            if (logEntryForTest == null)
            {
                logEntryForTest = Instantiate(logEntryPrefab, scrollRect.content);
                contentForTest = logEntryForTest.transform.Find("Text").GetComponent<TMP_Text>();
            }

            logEntryForTest.transform.SetParent(scrollRect.content, false);
            logEntryForTest.gameObject.SetActive(true);

            base.ForceRebuildLayoutAndResetTransitionTarget();

            contentDefaultWidth = contentForTest.GetComponent<RectTransform>().rect.width;
            logEntryForTest.gameObject.SetActive(false);
            logEntryForTest.transform.SetParent(scrollRect.transform, false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            myPanel.GetComponent<Button>().onClick.RemoveListener(Hide);
            closeButton.onClick.RemoveListener(Hide);

            gameState.gameStarted.RemoveListener(Clear);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            gameState.RemoveRestorable(this);
        }

        private void OnDialogueChanged(DialogueChangedData data)
        {
            if (data.dialogueData.needInterpolate)
            {
                logEntriesRestoreData.Add(new LogEntryRestoreData(data.displayData, logEntries.Count));
            }

            if (!gameState.isUpgrading)
            {
                AddEntry(data.nodeRecord, data.checkpointOffset, data.dialogueData, data.displayData);
            }
        }

        private void AddEntry(NodeRecord nodeRecord, long checkpointOffset, ReachedDialogueData dialogueData,
            DialogueDisplayData displayData)
        {
            var text = displayData.FormatNameDialogue();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // TODO: Refresh heights when locale changes
            var height = contentForTest.GetPreferredValues(text, contentDefaultWidth, 0).y;
            var cnt = logEntries.Count;
            var prefixHeight = height + (cnt > 0 ? logEntries[cnt - 1].prefixHeight : 0);
            logEntries.Add(new LogEntry(height, prefixHeight, nodeRecord.offset, checkpointOffset, dialogueData,
                displayData));

            if (!RestrainLogEntryNum(maxLogEntryNum))
            {
                scrollRect.totalCount = logEntries.Count;
            }
        }

        private bool RestrainLogEntryNum(int num)
        {
            int cnt = logEntries.Count;
            if (cnt <= num) return false;
            RemoveRange(0, cnt - num);
            return true;
        }

        private void RemoveRange(int index, int count)
        {
            logEntries.RemoveRange(index, count);
            for (int i = index; i < logEntries.Count; ++i)
            {
                logEntries[i].prefixHeight = logEntries[i].height + (i > 0 ? logEntries[i - 1].prefixHeight : 0);
            }

            logEntriesRestoreData.RemoveAll(x => x.index >= index && x.index < index + count);
            logEntriesRestoreData.ForEach(x =>
            {
                if (x.index >= index) x.index -= count;
            });

            scrollRect.totalCount = logEntries.Count;
            scrollRect.RefillCellsFromEnd();
        }

        public void Clear()
        {
            RemoveRange(0, logEntries.Count);
        }

        public LogEntry GetRandomLogEntry(System.Random random)
        {
            return random.Next(logEntries);
        }

        #region LoopScrollRect

        public Vector2 GetItemsSize(int itemsCount)
        {
            if (itemsCount <= 0) return new Vector2(0, 0);
            itemsCount = Mathf.Min(itemsCount, logEntries.Count);
            return new Vector2(0, logEntries[itemsCount - 1].prefixHeight);
        }

        private readonly Stack<Transform> pool = new Stack<Transform>();

        public GameObject GetObject(int index)
        {
            if (pool.Count == 0)
            {
                var element = Instantiate(logEntryPrefab, scrollRect.content);
                return element.gameObject;
            }

            Transform candidate = pool.Pop();
            candidate.SetParent(scrollRect.content, false);
            var go = candidate.gameObject;
            go.SetActive(true);
            return go;
        }

        public void ReturnObject(Transform trans)
        {
            trans.gameObject.SetActive(false);
            trans.SetParent(scrollRect.transform, false);
            pool.Push(trans);
        }

        public void ProvideData(Transform transform, int idx)
        {
            var logEntry = logEntries[idx];
            UnityAction onGoBackButtonClicked = () => MoveBack(logEntry, idx);

            UnityAction onPlayVoiceButtonClicked = null;
            var voices = logEntry.dialogueData.voices;
            if (GameCharacterController.CanPlayVoice(voices))
            {
                onPlayVoiceButtonClicked = () => GameCharacterController.ReplayVoice(voices);
            }

            var logEntryController = transform.GetComponent<LogEntryController>();
            logEntryController.Init(logEntry.displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked,
                logEntry.height);
        }

        #endregion

        public void MoveBackWithCallback(LogEntry logEntry, Action onFinish)
        {
            var nodeRecord = checkpointManager.GetNodeRecord(logEntry.nodeOffset);
            gameState.MoveBackTo(nodeRecord, logEntry.checkpointOffset, logEntry.dialogueData.dialogueIndex);
            Hide(onFinish);
        }

        private int selectedLogEntryIndex = -1;

        private void MoveBack(LogEntry logEntry, int index)
        {
            if (index == selectedLogEntryIndex)
            {
                selectedLogEntryIndex = -1;
                Alert.Show(
                    null,
                    "log.moveback.confirm",
                    () => MoveBackWithCallback(logEntry, null),
                    null,
                    "LogMoveBack"
                );
            }
            else
            {
                selectedLogEntryIndex = index;
            }
        }

        public override void Show(Action onFinish)
        {
            if (configManager.GetInt(LogViewFirstShownKey) == 0)
            {
                Alert.Show("log.first.hint");
                configManager.SetInt(LogViewFirstShownKey, 1);
            }

            base.Show(onFinish);

            scrollRect.RefillCellsFromEnd();
            scrollRect.verticalNormalizedPosition = 1f;
            selectedLogEntryIndex = -1;
        }

        private const float MaxScrollDownIdleTime = 0.2f;
        private float scrollDownIdleTime;

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();

            var delta = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (delta < -1e-3f)
            {
                // If the content does not extend beyond the viewport, immediately hide log view
                // In this case, verticalNormalizedPosition is always 0
                scrollRect.GetVerticalOffsetAndSize(out var contentHeight, out _);
                if (contentHeight < scrollRect.viewport.rect.height)
                {
                    Hide();
                    return;
                }

                // Otherwise, the first scrolling down stops when reaches the bottom,
                // and the second scrolling down hides log view
                // verticalNormalizedPosition can be > 1
                if (scrollDownIdleTime > MaxScrollDownIdleTime && scrollRect.verticalNormalizedPosition > 1f - 1e-3f)
                {
                    Hide();
                    return;
                }

                scrollDownIdleTime = 0f;
            }
            else if (delta > 1e-3f)
            {
                scrollDownIdleTime = 0f;
            }
            else
            {
                scrollDownIdleTime += Time.unscaledDeltaTime;
            }

            // TODO: fully support keyboard navigation
            if (Keyboard.current?[Key.UpArrow].isPressed == true)
            {
                Cursor.visible = false;
                scrollRect.velocity += scrollRect.scrollSensitivity * Vector2.down;
            }

            if (Keyboard.current?[Key.DownArrow].isPressed == true)
            {
                Cursor.visible = false;
                scrollRect.velocity += scrollRect.scrollSensitivity * Vector2.up;
            }
        }

        #region Restoration

        public string restorableName => "LogController";

        [Serializable]
        private class LogControllerRestoreData : IRestoreData
        {
            public readonly List<LogEntryRestoreData> logEntriesRestoreData;

            public LogControllerRestoreData(List<LogEntryRestoreData> logEntriesRestoreData)
            {
                this.logEntriesRestoreData = logEntriesRestoreData;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new LogControllerRestoreData(logEntriesRestoreData);
        }

        public void Restore(IRestoreData restoreData)
        {
            logEntriesRestoreData.Clear();
            Clear();
            if (gameState.isUpgrading)
            {
                return;
            }

            // TODO: In each checkpoint, only save logEntriesRestoreData from the last checkpoint
            // Or we can use a binary indexed tree to store them
            var data = restoreData as LogControllerRestoreData;
            logEntriesRestoreData.AddRange(data.logEntriesRestoreData);

            var i = 0;
            var logEntryRestoreData = logEntriesRestoreData.FirstOrDefault();
            foreach (var pos in gameState.GetDialogueHistory(maxLogEntryNum))
            {
                var dialogueData = checkpointManager.GetReachedDialogueData(pos.nodeRecord.name, pos.dialogueIndex);
                DialogueDisplayData displayData = null;

                if (logEntryRestoreData != null && logEntryRestoreData.index == logEntries.Count)
                {
                    displayData = logEntryRestoreData.displayData;
                    ++i;
                    logEntryRestoreData = i < logEntriesRestoreData.Count ? logEntriesRestoreData[i] : null;
                }

                if (displayData == null)
                {
                    var node = gameState.GetNode(pos.nodeRecord.name, true);
                    var entry = node.GetDialogueEntryAt(pos.dialogueIndex);
                    displayData = entry.GetDisplayData();
                }

                AddEntry(pos.nodeRecord, pos.checkpointOffset, dialogueData, displayData);
            }
        }

        #endregion
    }
}
