using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class LogEntryController : MonoBehaviour
    {
        private TextProxy textProxy;
        private Button goBackButton;
        private RectTransform buttons;
        private Button playVoiceButton;
        private DialogueDisplayData displayData;
        public float height { get; private set; }
        private bool inited;

        private void InitReferences()
        {
            if (inited) return;
            textProxy = transform.Find("Text").GetComponent<TextProxy>();
            textProxy.Init();
            goBackButton = transform.Find("Text/GoBackButton").GetComponent<Button>();
            buttons = transform.Find("Buttons").GetComponent<RectTransform>();
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

        // OnEnable and I18n.LocaleChanged are handled by LogController
        private void UpdateText()
        {
            if (!inited) return;
            textProxy.text = displayData.FormatNameDialogue();

            if (playVoiceButton.gameObject.activeSelf)
            {
                needUpdateButtonsPosition = true;
            }
        }

        bool needUpdateButtonsPosition;

        private void LateUpdate()
        {
            if (needUpdateButtonsPosition)
            {
                float y = textProxy.GetFirstCharacterCenterY();
                buttons.offsetMin = new Vector2(0f, y);
                buttons.offsetMax = new Vector2(100f, y);
                needUpdateButtonsPosition = false;
            }
        }
    }
}
