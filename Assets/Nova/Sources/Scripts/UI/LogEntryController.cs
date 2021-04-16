using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class LogEntryController : MonoBehaviour
    {
        [HideInInspector] public int logEntryIndex;

        private TextProxy textProxy;
        private Button goBackButton;
        private Button playVoiceButton;
        private DialogueDisplayData displayData;
        private bool inited;

        private void InitReferences()
        {
            if (inited) return;
            textProxy = transform.Find("Text").GetComponent<TextProxy>();
            textProxy.Init();
            goBackButton = transform.Find("Text/GoBackButton").GetComponent<Button>();
            var buttons = transform.Find("Buttons");
            playVoiceButton = buttons.Find("PlayVoiceButton").GetComponent<Button>();
            inited = true;
        }

        private static void InitButton(Button button, UnityAction onClickAction)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (onClickAction == null)
            {
                button.gameObject.SetActive(false);
            }
            else
            {
                button.onClick.AddListener(onClickAction);
            }
        }

        private UnityAction<int> onGoBackButtonClicked;

        private void OnGoBackButtonClicked()
        {
            onGoBackButtonClicked?.Invoke(logEntryIndex);
        }

        /// <summary>
        /// Initialize the log entry prefab
        /// </summary>
        /// <param name="displayData"></param>
        /// <param name="onGoBackButtonClicked">The action to perform when the go back button clicked</param>
        /// <param name="onPlayVoiceButtonClicked">The action to perform when the play voice button clicked</param>
        /// <param name="logEntryIndex"></param>
        public void Init(DialogueDisplayData displayData, UnityAction<int> onGoBackButtonClicked,
            UnityAction onPlayVoiceButtonClicked, int logEntryIndex)
        {
            InitReferences();
            this.logEntryIndex = logEntryIndex;
            this.onGoBackButtonClicked = onGoBackButtonClicked;
            InitButton(goBackButton, OnGoBackButtonClicked);
            InitButton(playVoiceButton, onPlayVoiceButtonClicked);
            this.displayData = displayData;
            UpdateText();
        }

        private void UpdateText()
        {
            if (!inited) return;
            textProxy.text = displayData.FormatNameDialogue();
        }

        private void OnEnable()
        {
            UpdateText();
            I18n.LocaleChanged.AddListener(UpdateText);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateText);
        }
    }
}