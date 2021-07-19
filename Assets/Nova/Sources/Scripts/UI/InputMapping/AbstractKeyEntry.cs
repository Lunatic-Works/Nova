using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class AbstractKeyEntry : MonoBehaviour
    {
        public Text label;
        public Image background;
        public Color selectedColor;
        public Color defaultColor;

        private InputMappingController controller;
        private AbstractKey key;

        private bool selected => controller != null && key == controller.currentSelectedKey;

        private void UpdateText()
        {
            label.text = I18n.__($"config.key.{Enum.GetName(typeof(AbstractKey), key)}");
        }

        public void RefreshColor()
        {
            background.color = selected ? selectedColor : defaultColor;
        }

        public void Init(InputMappingController controller, AbstractKey key)
        {
            this.controller = controller;
            this.key = key;
            UpdateText();
            RefreshColor();
        }

        private void OnEnable()
        {
            UpdateText();
            RefreshColor();
            I18n.LocaleChanged.AddListener(UpdateText);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateText);
        }

        public void SelectCurrentKey()
        {
            controller.currentSelectedKey = key;
        }
    }
}