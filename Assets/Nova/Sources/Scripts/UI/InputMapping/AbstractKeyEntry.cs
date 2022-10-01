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

        private bool selected => controller != null && key == controller.currentAbstractKey;

        private void UpdateText()
        {
            label.text = I18n.__($"config.key.{Enum.GetName(typeof(AbstractKey), key)}");
        }

        public void Refresh()
        {
            background.color = selected ? selectedColor : defaultColor;
        }

        public void Init(InputMappingController controller, AbstractKey key)
        {
            this.controller = controller;
            this.key = key;
            UpdateText();
            Refresh();
        }

        private void OnEnable()
        {
            UpdateText();
            Refresh();
            I18n.LocaleChanged.AddListener(UpdateText);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateText);
        }

        public void Select()
        {
            controller.currentAbstractKey = key;
        }
    }
}
