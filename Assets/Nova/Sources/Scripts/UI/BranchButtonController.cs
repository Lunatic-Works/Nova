using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class BranchButtonController : MonoBehaviour
    {
        public Text text;
        public Image image;
        public Button button;

        private Dictionary<SystemLanguage, string> displayTexts;

        public void Init(Dictionary<SystemLanguage, string> displayTexts, BranchImageInformation imageInfo, string imageFolder, UnityAction onClick, bool interactable)
        {
            this.displayTexts = displayTexts;
            UpdateText();

            if (imageInfo != null)
            {
                var layoutElement = gameObject.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;

                image.type = Image.Type.Simple;
                image.alphaHitTestMinimumThreshold = 0.5f;
                // TODO: preload
                image.sprite = AssetLoader.Load<Sprite>(System.IO.Path.Combine(imageFolder, imageInfo.name));
                image.SetNativeSize();
                transform.localPosition = new Vector3(imageInfo.positionX, imageInfo.positionY, 0f);
                transform.localScale = new Vector3(imageInfo.scale, imageInfo.scale, 1f);
            }

            button.onClick.AddListener(onClick);
            button.interactable = interactable;
        }

        private void UpdateText()
        {
            if (displayTexts == null)
            {
                text.text = "";
            }
            else
            {
                text.text = I18n.__(displayTexts);
            }
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