using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class LogEntryController : MonoBehaviour
    {
        private TextProxy textProxy;
        private Button goBackButton;
        private Button playVoiceButton;
        private DialogueDisplayData displayData;
        private bool inited;

        #region Layout

        private float height;
        public float minWidth => -1;
        public float preferredWidth => -1;
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => height;
        public float flexibleHeight => -1;
        public int layoutPriority => 1; // override VerticalLayoutGroup

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }

        #endregion

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

        private static void InitButton(Button button, UnityAction onClick)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (onClick == null)
            {
                button.gameObject.SetActive(false);
            }
            else
            {
                button.gameObject.SetActive(true);
                button.onClick.AddListener(onClick);
            }
        }

        public void Init(DialogueDisplayData displayData, UnityAction onGoBackButtonClicked,
            UnityAction onPlayVoiceButtonClicked, float height)
        {
            InitReferences();
            InitButton(goBackButton, onGoBackButtonClicked);
            InitButton(playVoiceButton, onPlayVoiceButtonClicked);
            this.displayData = displayData;
            UpdateText();
            this.height = height;
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
