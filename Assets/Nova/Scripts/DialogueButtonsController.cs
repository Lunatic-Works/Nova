using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class DialogueButtonsController : MonoBehaviour
    {
        public DialogueBoxController dialogueBoxController;

        private Button quickSaveButton;
        private Button quickLoadButton;
        private Button saveButton;
        private Button loadButton;
        private Button autoButton;
        private Button skipButton;
        private Button logButton;
        // TODO config button

        private void Awake()
        {
            quickSaveButton = transform.Find("QuickSave").GetComponent<Button>();
            quickLoadButton = transform.Find("QuickLoad").GetComponent<Button>();
            saveButton = transform.Find("Save").GetComponent<Button>();
            loadButton = transform.Find("Load").GetComponent<Button>();
            autoButton = transform.Find("Auto").GetComponent<Button>();
            skipButton = transform.Find("Skip").GetComponent<Button>();
            logButton = transform.Find("Log").GetComponent<Button>();

            autoButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Auto; });
            skipButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Skip; });

            quickSaveButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Normal; });
            quickLoadButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Normal; });
            saveButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Normal; });
            loadButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Normal; });
            logButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Normal; });
        }
    }
}