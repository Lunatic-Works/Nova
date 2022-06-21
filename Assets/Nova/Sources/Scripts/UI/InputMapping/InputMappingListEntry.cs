using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class InputMappingListEntry : MonoBehaviour
    {
        public Text label;
        public Color defaultColor;
        public Color activeColor;

        private InputMappingController controller;

        public InputBindingData bindingData { get; private set; }

        private void Awake()
        {
            label.color = defaultColor;
        }

        public void RefreshDisplay()
        {
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            bindingData.RefreshEndIndex();
            label.text = bindingData.displayString;
        }

        public void Init(InputMappingController controller, InputBindingData bindingData)
        {
            this.controller = controller;
            this.bindingData = bindingData;
            RefreshDisplay();
        }

        public void Delete()
        {
            controller.DeleteCompoundKey(bindingData);
        }

        private bool isModifying
        {
            set => label.color = value ? activeColor : defaultColor;
        }

        public void TriggerModify()
        {
            isModifying = true;
            controller.StartModifyBinding(this);
        }

        public void FinishModify()
        {
            isModifying = false;
        }
    }
}