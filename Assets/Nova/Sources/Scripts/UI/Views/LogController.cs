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
    using NodeHistoryEntry = KeyValuePair<string, int>;

    public class LogController : ViewControllerBase, IRestorable, LoopScrollPrefabSource, LoopScrollDataSource,
        LoopScrollSizeHelper
    {
        [Serializable]
        private class LogParam
        {
            public readonly DialogueDisplayData displayData;
            public readonly NodeHistoryEntry nodeHistoryEntry;
            public readonly int dialogueIndex;
            public readonly IReadOnlyDictionary<string, VoiceEntry> voices;
            public int logEntryIndex;

            public LogParam(DialogueDisplayData displayData, NodeHistoryEntry nodeHistoryEntry, int dialogueIndex,
                IReadOnlyDictionary<string, VoiceEntry> voices, int logEntryIndex)
            {
                this.displayData = displayData;
                this.nodeHistoryEntry = nodeHistoryEntry;
                this.dialogueIndex = dialogueIndex;
                this.voices = voices;
                this.logEntryIndex = logEntryIndex;
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
        private readonly List<float> logHeights = new List<float>();
        private readonly List<float> logPrefixHeights = new List<float>();

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
                logEntryForTest = Instantiate(logEntryPrefab);
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

        private void OnDialogueChanged(DialogueChangedData dialogueChangedData)
        {
            AddEntry(new LogParam(dialogueChangedData.displayData, dialogueChangedData.nodeHistoryEntry,
                dialogueChangedData.dialogueIndex, dialogueChangedData.voicesNextDialogue, logParams.Count));
        }

        private bool TryGetCheckpoint(LogParam logParam, bool isLatest, out GameStateCheckpoint checkpoint)
        {
            ulong nodeHistoryHash;
            if (isLatest)
            {
                nodeHistoryHash = gameState.nodeHistory.Hash;
            }
            else
            {
                var backNodeIndex = gameState.nodeHistory.FindLastIndex(x => x.Equals(logParam.nodeHistoryEntry));
                nodeHistoryHash = gameState.nodeHistory.GetHashULong(0, backNodeIndex + 1, logParam.dialogueIndex + 1);
            }

            var entry = checkpointManager.GetReached(nodeHistoryHash, logParam.nodeHistoryEntry.Key,
                logParam.dialogueIndex);
            if (entry is GameStateCheckpoint _checkpoint)
            {
                checkpoint = _checkpoint;
                return true;
            }
            else
            {
                checkpoint = null;
                return false;
            }
        }

        private void AddEntry(LogParam logParam)
        {
            var text = logParam.displayData.FormatNameDialogue();
            var isCheckpoint = TryGetCheckpoint(logParam, true, out _);
            if (string.IsNullOrEmpty(text))
            {
                if (isCheckpoint)
                {
                    checkpointLogParam = null;
                }

                return;
            }

            if (isCheckpoint || checkpointLogParam == null)
            {
                checkpointLogParam = logParam;
            }

            logParams.Add(logParam);

            var height = contentForTest.GetPreferredValues(text, contentDefaultWidth, 0).y;
            logHeights.Add(height);
            var cnt = logPrefixHeights.Count;
            logPrefixHeights.Add(height + (cnt > 0 ? logPrefixHeights[cnt - 1] : 0));

            if (!RestrainLogEntryNum(maxLogEntryNum))
                scrollRect.totalCount = logParams.Count;
        }

        private bool RestrainLogEntryNum(int num)
        {
            int cnt = logParams.Count;
            if (cnt <= num) return false;
            RemoveRange(0, cnt - num);
            return true;
        }

        private void RemoveRange(int index, int count)
        {
            logParams.RemoveRange(index, count);
            for (int i = index; i < logParams.Count; ++i)
            {
                logParams[i].logEntryIndex = i;
            }

            logHeights.RemoveRange(index, count);
            logPrefixHeights.RemoveRange(index, count);
            for (int i = index; i < logHeights.Count; ++i)
            {
                logPrefixHeights[i] = logHeights[i] + (i > 0 ? logPrefixHeights[i - 1] : 0);
            }

            scrollRect.totalCount = logParams.Count;
            scrollRect.RefillCellsFromEnd();
        }

        public void Clear()
        {
            RemoveRange(0, logParams.Count);
        }

        #region LoopScrollRect

        public Vector2 GetItemsSize(int itemsCount)
        {
            if (itemsCount <= 0) return new Vector2(0, 0);
            itemsCount = Mathf.Min(itemsCount, logPrefixHeights.Count);
            return new Vector2(0, logPrefixHeights[itemsCount - 1]);
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
            var logParam = logParams[idx];

            UnityAction onGoBackButtonClicked = () =>
                OnGoBackButtonClicked(logParam.nodeHistoryEntry, logParam.dialogueIndex, logParam.logEntryIndex);

            UnityAction onPlayVoiceButtonClicked = null;
            if (logParam.voices.Any())
            {
                onPlayVoiceButtonClicked = () => OnPlayVoiceButtonClicked(logParam.voices);
            }

            var logEntry = transform.GetComponent<LogEntryController>();
            logEntry.Init(logParam.displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked, logHeights[idx]);
        }

        #endregion

        private void _onGoBackButtonClicked(NodeHistoryEntry nodeHistoryEntry, int dialogueIndex)
        {
            gameState.MoveBackTo(nodeHistoryEntry, dialogueIndex);
            // Debug.Log($"Remaining log entries count: {logEntries.Count}");
            if (hideOnGoBackButtonClicked)
            {
                Hide();
            }
        }

        private int selectedLogEntryIndex = -1;

        private void OnGoBackButtonClicked(NodeHistoryEntry nodeHistoryEntry, int dialogueIndex, int logEntryIndex)
        {
            if (logEntryIndex == selectedLogEntryIndex)
            {
                selectedLogEntryIndex = -1;
                Alert.Show(
                    null,
                    "log.back.confirm",
                    () => _onGoBackButtonClicked(nodeHistoryEntry, dialogueIndex),
                    null,
                    "LogBack"
                );
            }
            else
            {
                selectedLogEntryIndex = logEntryIndex;
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
            public readonly List<LogParam> logParams;

            public LogControllerRestoreData(List<LogParam> logParams)
            {
                this.logParams = logParams;
            }
        }

        public IRestoreData GetRestoreData()
        {
            if (logParams.Count == 0 || checkpointLogParam == null)
            {
                return new LogControllerRestoreData(new List<LogParam>());
            }

            int checkpointIndex = logParams.IndexOf(checkpointLogParam);
            if (checkpointIndex < 0)
            {
                checkpointIndex = 0;
            }

            return new LogControllerRestoreData(logParams.GetRange(checkpointIndex,
                logParams.Count - checkpointIndex));
        }

        public void Restore(IRestoreData restoreData)
        {
            var allParams = new List<LogParam>();
            var data = restoreData as LogControllerRestoreData;
            while (data.logParams.Count > 0)
            {
                for (int i = data.logParams.Count - 1; i >= 0; i--)
                {
                    allParams.Add(data.logParams[i]);
                    if (allParams.Count >= maxLogEntryNum)
                    {
                        break;
                    }
                }

                this.RuntimeAssert(TryGetCheckpoint(data.logParams[0], false, out var entry),
                    "The earliest log in each restore data must point at another checkpoint.");
                data = entry.restoreDatas[restorableName] as LogControllerRestoreData;
            }

            Clear();
            for (int i = allParams.Count - 1; i >= 0; i--)
            {
                allParams[i].logEntryIndex = i;
                AddEntry(allParams[i]);
            }
        }

        #endregion
    }
}
