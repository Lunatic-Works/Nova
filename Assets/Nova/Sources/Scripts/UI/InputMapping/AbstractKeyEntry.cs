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

        public AbstractKey key { get; private set; }

        private void Awake()
        {
            RefreshColor();
        }

        public void RefreshColor()
        {
            background.color = selected ? selectedColor : defaultColor;
        }

        public bool selected => controller != null && key == controller.currentSelectedKey;

        public void Init(InputMappingController controller, string text, AbstractKey key)
        {
            this.controller = controller;
            label.text = text;
            this.key = key;
            RefreshColor();
        }

        public void SelectCurrentKey()
        {
            controller.currentSelectedKey = key;
        }
    }
}