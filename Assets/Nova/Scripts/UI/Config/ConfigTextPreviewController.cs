using System.Linq;
using TMPro;
using UnityEngine;

namespace Nova
{
    public class ConfigTextPreviewController : MonoBehaviour
    {
        public DialogueTextController textPreview;
        public NovaAnimation textPreviewAnimation;
        public TextSpeedConfigReader textSpeed;
        private float currentTextPreviewTimeLeft = 0;
        public float autoDelay;

        private static readonly string[] TextPreviewKeys =
            {"config.textpreview.1", "config.textpreview.2", "config.textpreview.3"};

        private int textPreviewIndex;

        private DialogueDisplayData GetPreviewDisplayData()
        {
            var displayNames =
                I18n.SupportedLocales.ToDictionary(x => x, x => I18n.__(x, "config.textpreview.name"));
            var dialogues =
                I18n.SupportedLocales.ToDictionary(x => x, x => I18n.__(x, TextPreviewKeys[textPreviewIndex]));
            return new DialogueDisplayData("Preview", displayNames, dialogues);
        }

        private void ResetTextPreview()
        {
            if (textPreviewAnimation == null) return;
            textPreviewAnimation.Stop();
            textPreview.Clear();
            var entry = textPreview.AddEntry(
                GetPreviewDisplayData(),
                TextAlignmentOptions.TopLeft,
                Color.black,
                Color.black,
                ""
            );
            var contentBox = entry.contentBox;
            var contentProxy = entry.contentProxy;
            var textAnimDuration = contentBox.textInfo.characterCount * textSpeed.perCharacterFadeInDuration;
            currentTextPreviewTimeLeft = textAnimDuration + autoDelay;
            textPreviewAnimation.Do(
                new TextFadeInAnimationProperty(contentProxy, 255),
                textAnimDuration
            );
            textPreviewIndex = (textPreviewIndex + 1) % TextPreviewKeys.Length;
        }

        private void UpdateTextPreview()
        {
            currentTextPreviewTimeLeft -= Time.deltaTime;
            if (currentTextPreviewTimeLeft < 0)
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