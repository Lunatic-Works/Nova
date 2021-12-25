// TODO: use circular buffer for log entries

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

    public class LogController : ViewControllerBase, IRestorable, LoopScrollPrefabSource, LoopScrollDataSource, LoopScrollSizeHelper
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

        public int maxLogEntryNum = 100;
        public LogEntryController logEntryPrefab;
        public Button closeButton;
        public bool hideOnGoBackButtonClicked;

        private const string LogViewFirstShownKey = ConfigManager.FirstShownKeyPrefix + "LogView";

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ConfigManager configManager;

        private LoopScrollRect scrollRect;
        private TMP_Text logEntryForTest;
        private GameObject logContent;
        private readonly List<LogParam> logParams = new List<LogParam>();
        private readonly List<float> logHeights = new List<float>();
        private readonly List<float> logPrefixHeights = new List<float>();
        private LogParam lastCheckpointLogParams;

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
            // fake content to test size
            var logEntry2 = Instantiate(logEntryPrefab, scrollRect.viewport);
            logEntry2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -500);
            logEntryForTest = logEntry2.transform.Find("Text").GetComponent<TMP_Text>();

            myPanel.GetComponent<Button>().onClick.AddListener(Hide);
            closeButton.onClick.AddListener(Hide);

            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            gameState.AddRestorable(this);

            lastCheckpointLogParams = null;
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
            if (TryGetCheckpoint(logParam, true, out _))
            {
                lastCheckpointLogParams = logParam;
            }

            if (string.IsNullOrEmpty(logParam.displayData.FormatNameDialogue()))
            {
                return;
            }

            logParams.Add(logParam);
            // TODO: better way to calc size
            string text = logParam.displayData.FormatNameDialogue();
            var rect = scrollRect.content.rect;
            rect.width -= 180 * 2;
            Vector2 size = logEntryForTest.GetPreferredValues(text, rect.width, rect.height);
            logHeights.Add(size.y);
            logPrefixHeights.Add(size.y + (logPrefixHeights.Count > 0 ? logPrefixHeights[logPrefixHeights.Count - 1] : 0));

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
            logHeights.RemoveRange(index, count);
            logPrefixHeights.Clear();
            for (int i = 0; i < logHeights.Count; i++)
            {
                logPrefixHeights[i] = logHeights[i];
                if (i > 0)
                    logPrefixHeights[i] += logPrefixHeights[i - 1];
            }
            // Refine log entry indices
            int cnt = logParams.Count;
            for (int i = index; i < cnt; ++i)
            {
                logParams[i].logEntryIndex = i;
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

        Stack<Transform> pool = new Stack<Transform>();
        public GameObject GetObject(int index)
        {
            if (pool.Count == 0)
            {
                var element = Instantiate(logEntryPrefab, scrollRect.content);
                return element.gameObject;
            }
            Transform candidate = pool.Pop();
            candidate.SetParent(scrollRect.content, false);
            candidate.gameObject.SetActive(true);
            return candidate.gameObject;
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
            
            UnityAction<int> onGoBackButtonClicked = logEntryIndex =>
                OnGoBackButtonClicked(logParam.nodeHistoryEntry, logParam.dialogueIndex, logEntryIndex);

            UnityAction onPlayVoiceButtonClicked = null;
            if (logParam.voices.Any())
            {
                onPlayVoiceButtonClicked = () => OnPlayVoiceButtonClicked(logParam.voices);
            }

            UnityAction<int> onPointerExit = logEntryIndex => OnPointerExit(logEntryIndex);

            var logEntry = transform.GetComponent<LogEntryController>();
            logEntry.Init(logParam.displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked, onPointerExit,
                logParam.logEntryIndex, logHeights[idx]);
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

        private int lastClickedLogIndex = -1;

        private void OnGoBackButtonClicked(NodeHistoryEntry nodeHistoryEntry, int dialogueIndex, int logEntryIndex)
        {
            if (logEntryIndex == lastClickedLogIndex)
            {
                Alert.Show(
                    null,
                    I18n.__("log.back.confirm"),
                    () => _onGoBackButtonClicked(nodeHistoryEntry, dialogueIndex),
                    null,
                    "LogBack"
                );
            }
            else
            {
                lastClickedLogIndex = logEntryIndex;
            }
        }

        private static void OnPlayVoiceButtonClicked(IReadOnlyDictionary<string, VoiceEntry> voiceEntries)
        {
            GameCharacterController.ReplayVoice(voiceEntries);
        }

        private void OnPointerExit(int logEntryIndex)
        {
            if (logEntryIndex == lastClickedLogIndex)
            {
                lastClickedLogIndex = -1;
            }
        }

        /// <summary>
        /// Show log panel
        /// </summary>
        public override void Show(Action onFinish)
        {
            if (configManager.GetInt(LogViewFirstShownKey) == 0)
            {
                Alert.Show(I18n.__("log.first.hint"));
                configManager.SetInt(LogViewFirstShownKey, 1);
            }

            base.Show(onFinish);

            scrollRect.RefillCellsFromEnd();
            lastClickedLogIndex = -1;
        }

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();

            if (Mouse.current?.scroll.ReadValue().y < 0 && Mathf.Approximately(scrollRect.verticalScrollbar.value, 0f))
            {
                Hide();
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
            if (logParams.Count == 0)
            {
                return new LogControllerRestoreData(new List<LogParam>());
            }

            int lastCheckpointParamsIndex = logParams.IndexOf(lastCheckpointLogParams);
            if (lastCheckpointParamsIndex < 0)
            {
                lastCheckpointParamsIndex = 0;
            }

            return new LogControllerRestoreData(logParams.GetRange(lastCheckpointParamsIndex,
                logParams.Count - lastCheckpointParamsIndex));
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
