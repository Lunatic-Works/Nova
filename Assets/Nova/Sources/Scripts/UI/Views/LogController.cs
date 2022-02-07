// TODO: use circular buffer for log entries

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    using NodeHistoryEntry = KeyValuePair<string, int>;

    public class LogController : ViewControllerBase, IRestorable
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

        private ScrollRect scrollRect;
        private GameObject logContent;
        private readonly List<LogEntryController> logEntries = new List<LogEntryController>();
        private readonly List<LogParam> logParams = new List<LogParam>();
        private LogParam lastCheckpointLogParams;

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
                dialogueChangedData.dialogueIndex, dialogueChangedData.voicesNextDialogue, logEntries.Count));
        }

        private bool TryGetCheckpoint(LogParam logParam, out GameStateCheckpoint checkpoint)
        {
            var backNodeIndex = gameState.nodeHistory.FindLastIndex(x => x.Equals(logParam.nodeHistoryEntry));
            var nodeHistoryHash =
                gameState.nodeHistory.GetHashULong(0, backNodeIndex + 1, logParam.dialogueIndex + 1);
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
            if (TryGetCheckpoint(logParam, out _))
            {
                lastCheckpointLogParams = logParam;
            }

            var logEntry = Instantiate(logEntryPrefab, logContent.transform);

            UnityAction<int> onGoBackButtonClicked = logEntryIndex =>
                OnGoBackButtonClicked(logParam.nodeHistoryEntry, logParam.dialogueIndex, logEntryIndex);

            UnityAction onPlayVoiceButtonClicked = null;
            if (logParam.voices.Any())
            {
                onPlayVoiceButtonClicked = () => OnPlayVoiceButtonClicked(logParam.voices);
            }

            logEntry.Init(logParam.displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked,
                logParam.logEntryIndex);

            logEntries.Add(logEntry);
            logParams.Add(logParam);
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
                Destroy(logEntries[i].gameObject);
            }

            logEntries.RemoveRange(index, count);
            logParams.RemoveRange(index, count);
            // Refine log entry indices
            int cnt = logEntries.Count;
            for (int i = index; i < cnt; ++i)
            {
                logEntries[i].logEntryIndex = i;
                logParams[i].logEntryIndex = i;
            }
        }

        public void Clear()
        {
            RemoveRange(0, logEntries.Count);
        }

        private void _onGoBackButtonClicked(NodeHistoryEntry nodeHistoryEntry, int dialogueIndex)
        {
            gameState.MoveBackTo(nodeHistoryEntry, dialogueIndex);
            // Debug.LogFormat("Remain log entries count: {0}", logEntries.Count);
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
            CharacterController.ReplayVoice(voiceEntries);
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

            scrollRect.verticalNormalizedPosition = 0.0f;

            base.Show(onFinish);

            scrollRect.verticalNormalizedPosition = 0.0f;
            lastClickedLogIndex = -1;
        }

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();

            if (Input.mouseScrollDelta.y < 0 && Mathf.Approximately(scrollRect.verticalScrollbar.value, 0f))
            {
                Hide();
            }
        }

        [Serializable]
        private class LogControllerRestoreData : IRestoreData
        {
            public readonly List<LogParam> logParams;

            public LogControllerRestoreData(List<LogParam> logParams)
            {
                this.logParams = logParams;
            }
        }

        public string restorableObjectName => "logController";

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

                this.RuntimeAssert(TryGetCheckpoint(data.logParams[0], out var entry),
                    "The earliest log in each restore data must point at another checkpoint.");
                data = entry.restoreDatas[restorableObjectName] as LogControllerRestoreData;
            }

            Clear();
            for (int i = allParams.Count - 1; i >= 0; i--)
            {
                allParams[i].logEntryIndex = i;
                AddEntry(allParams[i]);
            }
        }
    }
}