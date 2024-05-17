using TMPro;
using UnityEngine;

namespace Nova
{
    public class ConfigTextPreviewController : MonoBehaviour
    {
        private DialogueTextController dialogueText;
        private NovaAnimation textAnimation;

        [HideInInspector] public float characterFadeInDuration;
        [HideInInspector] public float autoDelay;

        private float currentTextPreviewTimeLeft;

        private static readonly string[] TextPreviewKeys =
            {"config.textpreview.1", "config.textpreview.2", "config.textpreview.3"};

        private int textPreviewIndex;

        private void Awake()
        {
            dialogueText = GetComponentInChildren<DialogueTextController>(true);
            textAnimation = Utils.FindNovaController().TextAnimation;
        }

        private DialogueDisplayData GetPreviewDisplayData()
        {
            var displayNames = I18n.GetLocalizedStrings("config.textpreview.name");
            var dialogues = I18n.GetLocalizedStrings(TextPreviewKeys[textPreviewIndex]);
            return new DialogueDisplayData(displayNames, dialogues);
        }

        private void ResetTextPreview()
        {
            if (textAnimation == null) return;
            textAnimation.Stop();
            dialogueText.Clear();
            var entry = dialogueText.AddEntry(
                GetPreviewDisplayData(),
                TextAlignmentOptions.TopLeft,
                Color.black,
                Color.black,
                null,
                DialogueEntryLayoutSetting.Default,
                0
            );
            var contentProxy = entry.contentProxy;
            var textDuration = characterFadeInDuration * contentProxy.GetPageCharacterCount();
            currentTextPreviewTimeLeft = textDuration + autoDelay;
            textAnimation.Then(new TextFadeInAnimationProperty(contentProxy), textDuration);
            textPreviewIndex = (textPreviewIndex + 1) % TextPreviewKeys.Length;
        }

        private void UpdateTextPreview()
        {
            currentTextPreviewTimeLeft -= Time.deltaTime;
            if (currentTextPreviewTimeLeft < 0f)
            {
                ResetTextPreview();
            }
        }

        private void OnEnable()
        {
            ResetTextPreview();
        }

        private void Update()
        {
            UpdateTextPreview();
        }
    }
}
