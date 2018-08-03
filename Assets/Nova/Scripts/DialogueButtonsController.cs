using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class DialogueButtonsController : MonoBehaviour
    {
        public DialogueBoxController dialogueBoxController;

        private Button saveButton;
        private Button loadButton;
        private Button autoButton;
        private Button skipButton;
        private Button logButton;

        private void Start () {
            saveButton = transform.Find("Save").gameObject.GetComponent<Button>();
            loadButton = transform.Find("Load").gameObject.GetComponent<Button>();
            autoButton = transform.Find("Auto").gameObject.GetComponent<Button>();
            skipButton = transform.Find("Skip").gameObject.GetComponent<Button>();
            logButton = transform.Find("Log").gameObject.GetComponent<Button>();

            autoButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Auto; });
            skipButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Skip; });

            saveButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Normal; });
            loadButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Normal; });
            logButton.onClick.AddListener(() => { dialogueBoxController.State = DialogueBoxState.Normal; });
        }
    }
}