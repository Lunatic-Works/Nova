using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Nova
{
    public class LogController : ViewControllerBase, IRestorable, LoopScrollPrefabSource, LoopScrollDataSource,
        LoopScrollSizeHelper
    {
        [Serializable]
        private class LogParam
        {
            public readonly long nodeOffset;
            public readonly int dialogueIndex;
            public readonly DialogueDisplayData displayData;
            public readonly IReadOnlyDictionary<string, VoiceEntry> voices;
            public int logEntryIndex;

            public LogParam(DialogueChangedData data, int index)
            {
                nodeOffset = data.nodeRecord.offset;
                dialogueIndex = data.dialogueData.dialogueIndex;
                displayData = data.displayData;
                voices = data.dialogueData.voices;
                logEntryIndex = index;
            }
        }

        private class LogEntry
        {
            public float height;
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

        public int maxLogEntryNum = 1000;
        public LogEntryController logEntryPrefab;
        public Button closeButton;
        public bool hideOnGoBackButtonClicked;

        private const string LogViewFirstShownKey = ConfigManager.FirstShownKeyPrefix + "LogView";

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ConfigManager configManager;

        private LoopScrollRect scrollRect;

        private LogEntryController logEntryForTest;
        private TMP_Text contentForTest;
        private float contentDefaultWidth;

        private readonly List<LogParam> logParams = new List<LogParam>();
        private readonly List<LogEntry> logEntries = new List<LogEntry>();

        // The first logParam at or after the last checkpoint
        private LogParam checkpointLogParam;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            configManager = controller.ConfigManager;

            scrollRect = myPanel.GetComponentInChildren<LoopScrollRect>();
            scrollRect.prefabSource = this;
            scrollRect.dataSource = this;
            scrollRect.sizeHelper = this;

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

            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            gameState.RemoveRestorable(this);
        }

        private void OnDialogueChanged(DialogueChangedData data)
        {
            if (data.dialogueData.needInterpolate)
            {
                logParams.Add(new LogParam(data, logEntries.Count));
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

            logParams.RemoveAll(logParam => logParam.logEntryIndex >= index && logParam.logEntryIndex < index + count);
            logParams.ForEach(logParam =>
            {
                if (logParam.logEntryIndex >= index) logParam.logEntryIndex -= count;
            });

            scrollRect.totalCount = logEntries.Count;
            scrollRect.RefillCellsFromEnd();
        }

        public void Clear()
        {
            RemoveRange(0, logEntries.Count);
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
            var dialogueData = logEntry.dialogueData;
            // var logParam = logParams[idx];
            UnityAction onGoBackButtonClicked = () =>
                OnGoBackButtonClicked(logEntry.nodeOffset, logEntry.checkpointOffset, dialogueData.dialogueIndex, idx);

            UnityAction onPlayVoiceButtonClicked = null;
            if (dialogueData.voices.Any())
            {
                onPlayVoiceButtonClicked = () => OnPlayVoiceButtonClicked(dialogueData.voices);
            }

            var logEntryController = transform.GetComponent<LogEntryController>();
            logEntryController.Init(logEntry.displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked,
                logEntry.height);
        }

        #endregion

        private void _onGoBackButtonClicked(long nodeOffset, long checkpointOffset, int dialogueIndex)
        {
            var nodeRecord = checkpointManager.GetNodeRecord(nodeOffset);
            gameState.MoveBackTo(nodeRecord, checkpointOffset, dialogueIndex);
            // Debug.Log($"Remaining log entries count: {logEntries.Count}");
            if (hideOnGoBackButtonClicked)
            {
                Hide();
            }
        }

        private int selectedLogEntryIndex = -1;

        private void OnGoBackButtonClicked(long nodeOffset, long checkpointOffset, int dialogueIndex, int index)
        {
            if (index == selectedLogEntryIndex)
            {
                selectedLogEntryIndex = -1;
                Alert.Show(
                    null,
                    "log.back.confirm",
                    () => _onGoBackButtonClicked(nodeOffset, checkpointOffset, dialogueIndex),
                    null,
                    "LogBack"
                );
            }
            else
            {
                selectedLogEntryIndex = index;
            }
        }

        private static void OnPlayVoiceButtonClicked(IReadOnlyDictionary<string, VoiceEntry> voiceEntries)
        {
            GameCharacterController.ReplayVoice(voiceEntries);
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
            public readonly IReadOnlyList<LogParam> logParams;

            public LogControllerRestoreData(IReadOnlyList<LogParam> logParams)
            {
                this.logParams = logParams;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new LogControllerRestoreData(logParams);
        }

        public void Restore(IRestoreData restoreData)
        {
            Clear();
            logParams.Clear();
            var data = restoreData as LogControllerRestoreData;
            logParams.AddRange(data.logParams);
            var i = 0;
            foreach (var pos in gameState.GetDialogueHistory(maxLogEntryNum))
            {
                var dialogueData = checkpointManager.GetReachedDialogueData(pos.nodeRecord.name, pos.dialogueIndex);
                DialogueDisplayData displayData;
                if (dialogueData.needInterpolate)
                {
                    displayData = logParams[i].displayData;
                    i++;
                }
                else
                {
                    var node = gameState.flowChartTree.GetNode(pos.nodeRecord.name);
                    var entry = node.GetDialogueEntryAt(pos.dialogueIndex);
                    displayData = entry.GetDisplayData();
                }

                AddEntry(pos.nodeRecord, pos.checkpointOffset, dialogueData, displayData);
            }
        }

        #endregion
    }
}
