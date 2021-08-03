// TODO: use circular buffer for log entries

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
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
        private ulong previousVariablesHash;
        private readonly List<LogEntryController> logEntries = new List<LogEntryController>();
        private readonly List<InitParams> logParams = new List<InitParams>();
        private InitParams lastCheckpointLogParamsRef;

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

            gameState.DialogueWillChange += OnDialogueWillChange;
            gameState.DialogueChanged += OnDialogueChanged;
            gameState.AddRestorable(this);

            closeButton.onClick.AddListener(Hide);

            previousVariablesHash = 0UL;

            lastCheckpointLogParamsRef = null;
        }

        private void OnDialogueWillChange()
        {
            previousVariablesHash = gameState.variables.hash;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            gameState.DialogueWillChange -= OnDialogueWillChange;
            gameState.DialogueChanged -= OnDialogueChanged;
            gameState.RemoveRestorable(this);
        }

        private void OnDialogueChanged(DialogueChangedData dialogueChangedData)
        {
            string currentNodeName = dialogueChangedData.nodeName;
            int currentDialogueIndex = dialogueChangedData.dialogueIndex;
            int logEntryIndex = logEntries.Count;
            var voices = dialogueChangedData.voicesForNextDialogue;

            AddEntry(new InitParams
            {
                displayData = dialogueChangedData.displayData,
                currentNodeName = currentNodeName,
                currentDialogueIndex = currentDialogueIndex,
                variablesHashBeforeChange = previousVariablesHash,
                voices = voices,
                logEntryIndex = logEntryIndex
            });
        }

        private void AddEntry(InitParams initParams)
        {
            if (checkpointManager.GetReached(initParams.currentNodeName, initParams.currentDialogueIndex,
                initParams.variablesHashBeforeChange) is GameStateCheckpoint)
            {
                lastCheckpointLogParamsRef = initParams;
            }

            var logEntry = Instantiate(logEntryPrefab, logContent.transform);

            UnityAction<int> onGoBackButtonClicked = logEntryIndex => OnGoBackButtonClicked(initParams.currentNodeName,
                initParams.currentDialogueIndex, logEntryIndex, initParams.variablesHashBeforeChange);

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

        private void _onGoBackButtonClicked(string nodeName, int dialogueIndex, int logEntryIndex, ulong variablesHash)
        {
            gameState.MoveBackTo(nodeName, dialogueIndex, variablesHash);
            // Debug.LogFormat("Remain log entries count: {0}", logEntries.Count);
            if (hideOnGoBackButtonClicked)
            {
                Hide();
            }
        }

        private int lastClickedLogIndex = -1;

        private void OnGoBackButtonClicked(string nodeName, int dialogueIndex, int logEntryIndex, ulong variablesHash)
        {
            if (logEntryIndex == lastClickedLogIndex)
            {
                Alert.Show(
                    null,
                    I18n.__("log.back.confirm"),
                    () => _onGoBackButtonClicked(nodeName, dialogueIndex, logEntryIndex, variablesHash),
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
            public string currentNodeName;
            public int currentDialogueIndex;
            public ulong variablesHashBeforeChange;
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

            int lastCheckpointParamsIndex = logParams.IndexOf(lastCheckpointLogParamsRef);
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
                var entry = checkpointManager.GetReached(lastParams.currentNodeName, lastParams.currentDialogueIndex,
                    lastParams.variablesHashBeforeChange) as GameStateCheckpoint;
                this.RuntimeAssert(entry != null,
                    "the earliest log in each restore data must be pointing at another checkpoint");
                curr = entry[restorableObjectName] as LogControllerRestoreData;
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