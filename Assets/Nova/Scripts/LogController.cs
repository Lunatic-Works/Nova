using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class LogController : MonoBehaviour
    {
        public GameObject LogEntryPrefab;

        private const string goBackLogComfirmText = "退回到这句话？";

        private GameState gameState;

        private GameObject logPanel;
        private ScrollRect scrollRect;
        private GameObject logContent;
        private readonly List<GameObject> logEntries = new List<GameObject>();

        private AlertController alertController;

        private void Awake()
        {
            logPanel = transform.Find("LogPanel").gameObject;
            scrollRect = logPanel.transform.Find("ScrollView").GetComponent<ScrollRect>();
            logContent = scrollRect.transform.Find("Viewport/Content").gameObject;
            alertController = GameObject.FindWithTag("Alert").GetComponent<AlertController>();
            gameState = Utils.FindGameController().GetComponent<GameState>();
            gameState.DialogueChanged += OnDialogueChanged;
            gameState.BookmarkWillLoad += OnBookmarkWillLoad;
        }

        private void OnDestroy()
        {
            gameState.DialogueChanged -= OnDialogueChanged;
            gameState.BookmarkWillLoad -= OnBookmarkWillLoad;
        }

        private void OnDialogueChanged(DialogueChangedData dialogueChangedData)
        {
            var logEntry = Instantiate(LogEntryPrefab);
            var logEntryController = logEntry.GetComponent<LogEntryController>();
            var logEntryIndex = logEntries.Count;
            var currentNodeName = dialogueChangedData.nodeName;
            var currentDialogueIndex = dialogueChangedData.dialogueIndex;
            var voices = dialogueChangedData.voicesForNextDialogue;

            // TODO Add favorite
            UnityAction onGoBackButtonClicked =
                () => OnGoBackButtonClicked(currentNodeName, currentDialogueIndex, logEntryIndex);

            UnityAction onPlayVoiceButtonClicked = null;
            if (voices.Any())
            {
                onPlayVoiceButtonClicked = () => OnPlayVoiceButtonClicked(voices);
            }

            UnityAction onAddFavoriteButtonClicked = null;

            logEntryController.Init(dialogueChangedData.text, onGoBackButtonClicked,
                onPlayVoiceButtonClicked, onAddFavoriteButtonClicked);
            logEntries.Add(logEntry);
            logEntry.transform.SetParent(logContent.transform);
        }

        public bool hideOnGoBackButtonClicked;

        private void RemoveLogEntriesRange(int startIndex, int endIndex)
        {
            for (var i = startIndex; i < endIndex; ++i)
            {
                Destroy(logEntries[i]);
            }

            logEntries.RemoveRange(startIndex, endIndex - startIndex);
        }

        private void _onGoBackButtonClicked(string nodeName, int dialogueIndex, int logEntryIndex)
        {
            RemoveLogEntriesRange(logEntryIndex, logEntries.Count);
            gameState.MoveBackTo(nodeName, dialogueIndex);
            Debug.Log(string.Format("Remain log entries count: {0}", logEntries.Count));
            if (hideOnGoBackButtonClicked)
            {
                Hide();
            }
        }

        private void OnGoBackButtonClicked(string nodeName, int dialogueIndex, int logEntryIndex)
        {
            alertController.Alert(null, goBackLogComfirmText,
                () => _onGoBackButtonClicked(nodeName, dialogueIndex, logEntryIndex));
        }

        private void OnPlayVoiceButtonClicked(IEnumerable<string> audioNames)
        {
            // TODO this is an implementation for debug, the behaviour is not what we want
            foreach (var audioName in audioNames)
            {
                var clip = AssetsLoader.GetAudioClip(audioName);
                AudioSource.PlayClipAtPoint(clip, new Vector3(0, 0, -10));
            }
        }

        private void OnBookmarkWillLoad(BookmarkWillLoadData data)
        {
            // clear all log entries
            RemoveLogEntriesRange(0, logEntries.Count);
        }

        /// <summary>
        /// Show log panel
        /// </summary>
        public void Show()
        {
            logPanel.SetActive(true);
            scrollRect.verticalNormalizedPosition = 0.0f;
        }

        /// <summary>
        /// Hide log panel
        /// </summary>
        public void Hide()
        {
            logPanel.SetActive(false);
        }
    }
}