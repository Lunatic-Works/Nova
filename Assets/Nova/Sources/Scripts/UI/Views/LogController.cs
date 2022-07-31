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
            public readonly DialogueDisplayData displayData;
            public readonly long nodeOffset;
            public readonly long checkpointOffset;
            public readonly int dialogueIndex;
            public readonly IReadOnlyDictionary<string, VoiceEntry> voices;
            public int logEntryIndex;

            public LogParam(DialogueChangedData data, int logEntryIndex)
            {
                this.displayData = data.displayData;
                this.nodeOffset = data.nodeRecord.offset;
                this.checkpointOffset = data.checkpointOffset;
                this.dialogueIndex = data.dialogueIndex;
                this.voices = data.voicesNextDialogue;
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

        private void OnDialogueChanged(DialogueChangedData dialogueChangedData)
        {
            AddEntry(new LogParam(dialogueChangedData, logParams.Count));
        }

        private void AddEntry(LogParam logParam)
        {
            var text = logParam.displayData.FormatNameDialogue();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            logParams.Add(logParam);

            var height = contentForTest.GetPreferredValues(text, contentDefaultWidth, 0).y;
            logHeights.Add(height);
            var cnt = logPrefixHeights.Count;
            logPrefixHeights.Add(height + (cnt > 0 ? logPrefixHeights[cnt - 1] : 0));

            if (!RestrainLogEntryNum(maxLogEntryNum))
            {
                scrollRect.totalCount = logParams.Count;
            }
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
            UnityAction onGoBackButtonClicked = () => OnGoBackButtonClicked(logParam);

            UnityAction onPlayVoiceButtonClicked = null;
            if (logParam.voices.Any())
            {
                onPlayVoiceButtonClicked = () => OnPlayVoiceButtonClicked(logParam.voices);
            }

            var logEntry = transform.GetComponent<LogEntryController>();
            logEntry.Init(logParam.displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked, logHeights[idx]);
        }

        #endregion

        private void _onGoBackButtonClicked(LogParam logParam)
        {
            var nodeRecord = checkpointManager.GetNodeRecord(logParam.nodeOffset);
            gameState.MoveBackTo(nodeRecord, logParam.checkpointOffset, logParam.dialogueIndex);
            // Debug.Log($"Remaining log entries count: {logEntries.Count}");
            if (hideOnGoBackButtonClicked)
            {
                Hide();
            }
        }

        private int selectedLogEntryIndex = -1;

        private void OnGoBackButtonClicked(LogParam logParam)
        {
            if (logParam.logEntryIndex == selectedLogEntryIndex)
            {
                selectedLogEntryIndex = -1;
                Alert.Show(
                    null,
                    "log.back.confirm",
                    () => _onGoBackButtonClicked(logParam),
                    null,
                    "LogBack"
                );
            }
            else
            {
                selectedLogEntryIndex = logParam.logEntryIndex;
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
            return new LogControllerRestoreData(logParams);
        }

        public void Restore(IRestoreData restoreData)
        {
            Clear();
            var data = restoreData as LogControllerRestoreData;
            foreach (var param in data.logParams)
            {
                AddEntry(param);
            }
        }

        #endregion
    }
}
