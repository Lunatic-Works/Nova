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
        public int maxLogEntryNum = 100;
        public LogEntryController logEntryPrefab;
        public Button closeButton;
        public bool hideOnGoBackButtonClicked;

        private const string LogViewFirstShownKey = ConfigViewController.FirstShownKeyPrefix + "LogView";

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ConfigManager configManager;

        private ScrollRect scrollRect;
        private GameObject logContent;
        private readonly List<LogEntryController> logEntries = new List<LogEntryController>();
        private readonly List<InitParams> logParams = new List<InitParams>();
        private InitParams lastCheckpointLogParams;

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
            AddEntry(new InitParams
            {
                displayData = dialogueChangedData.displayData,
                nodeHistoryEntry = dialogueChangedData.nodeHistoryEntry,
                dialogueIndex = dialogueChangedData.dialogueIndex,
                voices = dialogueChangedData.voicesForNextDialogue,
                logEntryIndex = logEntries.Count
            });
        }

        private void AddEntry(InitParams initParams)
        {
            var backNodeIndex = gameState.nodeHistory.FindLastIndex(x => x.Equals(initParams.nodeHistoryEntry));
            if (checkpointManager.GetReached(gameState.nodeHistory.GetHashULong(0, backNodeIndex + 1),
                    initParams.dialogueIndex) is GameStateCheckpoint)
            {
                lastCheckpointLogParams = initParams;
            }

            var logEntry = Instantiate(logEntryPrefab, logContent.transform);

            UnityAction<int> onGoBackButtonClicked = logEntryIndex =>
                OnGoBackButtonClicked(initParams.nodeHistoryEntry, initParams.dialogueIndex, logEntryIndex);

            UnityAction onPlayVoiceButtonClicked = null;
            if (initParams.voices.Any())
            {
                onPlayVoiceButtonClicked = () => OnPlayVoiceButtonClicked(initParams.voices);
            }

            logEntry.Init(initParams.displayData, onGoBackButtonClicked, onPlayVoiceButtonClicked,
                initParams.logEntryIndex);

            logEntries.Add(logEntry);
            logParams.Add(initParams);
            RestrainLogEntryNum(maxLogEntryNum);
        }

        private void RestrainLogEntryNum(int num)
        {
            int cnt = logEntries.Count;
            if (cnt <= num) return;
            RemoveLogEntriesRange(0, cnt - num);
        }

        private void RemoveLogEntriesRange(int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; ++i)
            {
                Destroy(logEntries[i].gameObject);
            }

            logEntries.RemoveRange(startIndex, endIndex - startIndex);
            logParams.RemoveRange(startIndex, endIndex - startIndex);
            // refine log entry index
            int cnt = logEntries.Count;
            for (int i = startIndex; i < cnt; ++i)
            {
                logEntries[i].logEntryIndex = i;
                logParams[i].logEntryIndex = i;
            }
        }

        public void Clear()
        {
            RemoveLogEntriesRange(0, logEntries.Count);
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
        private class InitParams
        {
            public DialogueDisplayData displayData;
            public NodeHistoryEntry nodeHistoryEntry;
            public int dialogueIndex;
            public Dictionary<string, VoiceEntry> voices;
            public int logEntryIndex;
        }

        [Serializable]
        private class LogControllerRestoreData : IRestoreData
        {
            public List<InitParams> logParams;
        }

        public string restorableObjectName => "logController";

        public IRestoreData GetRestoreData()
        {
            if (logParams.Count == 0)
            {
                return new LogControllerRestoreData {logParams = new List<InitParams>()};
            }

            int lastCheckpointParamsIndex = logParams.IndexOf(lastCheckpointLogParams);
            if (lastCheckpointParamsIndex < 0)
            {
                lastCheckpointParamsIndex = 0;
            }

            return new LogControllerRestoreData
            {
                logParams = logParams.GetRange(lastCheckpointParamsIndex, logParams.Count - lastCheckpointParamsIndex)
            };
        }

        public void Restore(IRestoreData restoreData)
        {
            var allParams = new List<InitParams>();
            var curr = restoreData as LogControllerRestoreData;
            while (curr.logParams.Count > 0)
            {
                for (int i = curr.logParams.Count - 1; i >= 0; i--)
                {
                    allParams.Add(curr.logParams[i]);
                    if (allParams.Count >= maxLogEntryNum)
                    {
                        break;
                    }
                }

                var lastParams = curr.logParams[0];
                var backNodeIndex = gameState.nodeHistory.FindLastIndex(x => x.Equals(lastParams.nodeHistoryEntry));
                var entry = checkpointManager.GetReached(gameState.nodeHistory.GetHashULong(0, backNodeIndex + 1),
                    lastParams.dialogueIndex) as GameStateCheckpoint;
                this.RuntimeAssert(entry != null,
                    "The earliest log in each restore data must point at another checkpoint.");
                curr = entry.restoreDatas[restorableObjectName] as LogControllerRestoreData;
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