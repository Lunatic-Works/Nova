using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    using VoiceEntries = Dictionary<string, VoiceEntry>;

    public class LogEntry
    {
        public readonly long nodeOffset;
        public readonly long checkpointOffset;
        public readonly int dialogueIndex;
        public readonly LogEntryController controller;

        public LogEntry(long nodeOffset, long checkpointOffset, int dialogueIndex, LogEntryController controller)
        {
            this.nodeOffset = nodeOffset;
            this.checkpointOffset = checkpointOffset;
            this.dialogueIndex = dialogueIndex;
            this.controller = controller;
        }
    }

    public class LogController : ViewControllerBase, IRestorable
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

        private ScrollRect scrollRect;
        private GameObject logContent;

        private readonly List<LogEntry> logEntries = new List<LogEntry>();
        private readonly List<LogEntryRestoreData> logEntriesRestoreData = new List<LogEntryRestoreData>();

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            configManager = controller.ConfigManager;

            scrollRect = myPanel.GetComponentInChildren<ScrollRect>();
            logContent = scrollRect.transform.Find("Viewport/Content").gameObject;

            myPanel.GetComponent<Button>().onClick.AddListener(Hide);
            closeButton.onClick.AddListener(Hide);

            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            gameState.AddRestorable(this);
        }

        protected override void Start()
        {
            base.Start();

            checkpointManager.Init();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            myPanel.GetComponent<Button>().onClick.RemoveListener(Hide);
            closeButton.onClick.RemoveListener(Hide);

            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            gameState.RemoveRestorable(this);
        }

        private void OnDialogueChanged(DialogueChangedData data)
        {
            if (data.dialogueData.needInterpolate)
            {
                logEntriesRestoreData.Add(new LogEntryRestoreData(data.displayData, logEntries.Count));
            }

            AddEntry(data.nodeRecord, data.checkpointOffset, data.dialogueData, data.displayData);
        }

        private void AddEntry(NodeRecord nodeRecord, long checkpointOffset, ReachedDialogueData dialogueData,
            DialogueDisplayData displayData)
        {
            var text = displayData.FormatNameDialogue();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var logEntryController = Instantiate(logEntryPrefab, logContent.transform);
            var logEntry = new LogEntry(nodeRecord.offset, checkpointOffset, dialogueData.dialogueIndex,
                logEntryController);

            UnityAction onGoBackButtonClicked = () => MoveBack(logEntry, logEntryController.GetInstanceID());

            UnityAction onPlayVoiceButtonClicked = null;
            if (GameCharacterController.CanPlayVoice(dialogueData.voices))
            {
                onPlayVoiceButtonClicked = () => GameCharacterController.ReplayVoice(dialogueData.voices);
            }

            logEntryController.Init(displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked);

            logEntries.Add(logEntry);
            RestrainLogEntryNum(maxLogEntryNum);
        }

        private void RestrainLogEntryNum(int num)
        {
            int cnt = logEntries.Count;
            if (cnt <= num) return;
            RemoveRange(0, cnt - num);
        }

        private void RemoveRange(int index, int count)
        {
            for (int i = index; i < index + count; ++i)
            {
                Destroy(logEntries[i].controller.gameObject);
            }

            logEntries.RemoveRange(index, count);

            logEntriesRestoreData.RemoveAll(x => x.index >= index && x.index < index + count);
            logEntriesRestoreData.ForEach(x =>
            {
                if (x.index >= index) x.index -= count;
            });
        }

        public void Clear()
        {
            RemoveRange(0, logEntries.Count);
        }

        public LogEntry GetRandomLogEntry(System.Random random)
        {
            return random.Next(logEntries);
        }

        public void MoveBackWithCallback(LogEntry logEntry, Action onFinish)
        {
            var nodeRecord = checkpointManager.GetNodeRecord(logEntry.nodeOffset);
            gameState.MoveBackTo(nodeRecord, logEntry.checkpointOffset, logEntry.dialogueIndex);
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

            scrollRect.verticalNormalizedPosition = 0.0f;
            selectedLogEntryIndex = -1;
        }

        private const float MaxScrollDownIdleTime = 0.2f;
        private float scrollDownIdleTime;

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();

            var delta = Input.mouseScrollDelta.y;
            if (delta < -1e-3f)
            {
                // If the content does not extend beyond the viewport, immediately hide log view
                if (scrollRect.content.rect.height < scrollRect.viewport.rect.height)
                {
                    Hide();
                    return;
                }

                // Otherwise, the first scrolling down stops when reaches the bottom,
                // and the second scrolling down hides log view
                if (scrollDownIdleTime > MaxScrollDownIdleTime && scrollRect.verticalNormalizedPosition < 1e-3f)
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
            if (Input.GetKey(KeyCode.UpArrow))
            {
                Cursor.visible = false;
                scrollRect.velocity += scrollRect.scrollSensitivity * Vector2.down;
            }

            if (Input.GetKey(KeyCode.DownArrow))
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
                    var node = gameState.GetNode(pos.nodeRecord.name);
                    var entry = node.GetDialogueEntryAt(pos.dialogueIndex);
                    displayData = entry.GetDisplayData();
                }

                AddEntry(pos.nodeRecord, pos.checkpointOffset, dialogueData, displayData);
            }
        }

        #endregion
    }
}
