using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    public class LogController : MonoBehaviour
    {
        public GameState gameState;

        public GameObject LogEntryPrefab;

        private GameObject logContent;

        private readonly List<GameObject> logEntries = new List<GameObject>();

        private void Awake()
        {
            logContent = transform.Find("ScrollView/Viewport/Content").gameObject;
            gameState.DialogueChanged.AddListener(OnDialogueChanged);
        }

        public bool hideOnStart;

        private void Start()
        {
            if (hideOnStart)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDialogueChanged(DialogueChangedEventData dialogueChangedEventData)
        {
            var logEntry = Instantiate(LogEntryPrefab);
            var logEntryController = logEntry.GetComponent<LogEntryController>();
            var logEntryIndex = logEntries.Count;
            var currentNodeName = dialogueChangedEventData.labelName;
            var currentDialogueIndex = dialogueChangedEventData.dialogueIndex;
            var voices = dialogueChangedEventData.voicesForNextDialogue;

            // TODO Add favorite
            UnityAction onGoBackButtonClicked =
                () => OnGoBackButtonClicked(currentNodeName, currentDialogueIndex, logEntryIndex);

            UnityAction onPlayVoiceButtonClicked = null;
            if (voices.Any())
            {
                onPlayVoiceButtonClicked = () => OnPlayVoiceButtonClicked(voices);
            }

            UnityAction onAddFavoriteButtonClicked = null;

            logEntryController.Init(dialogueChangedEventData.text, onGoBackButtonClicked,
                onPlayVoiceButtonClicked, onAddFavoriteButtonClicked);
            logEntries.Add(logEntry);
            logEntry.transform.SetParent(logContent.transform);
        }

        public bool hideOnGoBackButtonClicked;

        private void OnGoBackButtonClicked(string nodeName, int dialogueIndex, int logEntryIndex)
        {
            for (var i = logEntryIndex; i < logEntries.Count; ++i)
            {
                Destroy(logEntries[i]);
            }

            logEntries.RemoveRange(logEntryIndex, logEntries.Count - logEntryIndex);
            gameState.MoveBackTo(nodeName, dialogueIndex);
            Debug.Log(string.Format("Remain log entries count: {0}", logEntries.Count));
            if (hideOnGoBackButtonClicked)
            {
                gameObject.SetActive(false);
            }
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
    }
}