using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class LogEntryController : MonoBehaviour
    {
        private TextProxy textProxy;
        private RectTransform buttonsRectTransform;
        private float buttonsOffsetMinX;
        private float buttonsOffsetMaxX;
        private Button goBackButton;
        private Button playVoiceButton;
        private DialogueDisplayData displayData;
        public float height { get; private set; }
        private bool inited;

        private void InitReferences()
        {
            if (inited) return;
            textProxy = transform.Find("Text").GetComponent<TextProxy>();
            textProxy.Init();
            buttonsRectTransform = transform.Find("Buttons").GetComponent<RectTransform>();
            buttonsOffsetMinX = buttonsRectTransform.offsetMin.x;
            buttonsOffsetMaxX = buttonsRectTransform.offsetMax.x;
            goBackButton = transform.Find("Text/GoBackButton").GetComponent<Button>();
            playVoiceButton = buttonsRectTransform.Find("PlayVoiceButton").GetComponent<Button>();
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

        private bool needUpdateButtonsPosition;

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

        private void LateUpdate()
        {
            if (needUpdateButtonsPosition)
            {
                textProxy.GetComponent<FontSizeReader>().UpdateValue();
                textProxy.ForceRefresh();
                var y = textProxy.GetFirstCharacterCenterY();
                buttonsRectTransform.offsetMin = new Vector2(buttonsOffsetMinX, y);
                buttonsRectTransform.offsetMax = new Vector2(buttonsOffsetMaxX, y);
                needUpdateButtonsPosition = false;
            }
        }
    }
}
