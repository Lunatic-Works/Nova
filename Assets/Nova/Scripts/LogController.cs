using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Nova;
using UnityEngine;
using UnityEngine.UI;

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

        private void OnDialogueChanged(DialogueChangedEventData dialogueChangedEventData)
        {
            var logEntry = Instantiate(LogEntryPrefab);
            var logEntryController = logEntry.GetComponent<LogEntryController>();
            // TODO unassigned action to log entry buttons
            logEntryController.Init(
                dialogueChangedEventData.text,
                () => { }, () => { }, () => { }
            );
            logEntries.Add(logEntry);
            logEntry.transform.SetParent(logContent.transform);
        }
    }
}