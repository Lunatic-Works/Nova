using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nova
{
    public class InputMappingListEntry : MonoBehaviour
    {
        public Text label;
        public Color defaultColor;
        public Color activeColor;

        private InputMappingController controller;

        public int index { get; private set; }
        public InputBinding binding { get; private set; }

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
            label.text = binding.ToString();
        }

        public void Init(InputMappingController controller, int index)
        {
            this.controller = controller;
            this.index = index;
            binding = controller.currentAction.bindings[index];
            RefreshDisplay();
        }

        public void Delete()
        {
            controller.DeleteCompoundKey(index);
        }

        private bool isModifying
        {
            set => label.color = value ? activeColor : defaultColor;
        }

        public void TriggerModify()
        {
            isModifying = true;
            controller.StartModifyCompoundKey(this);
        }

        public void FinishModify()
        {
            isModifying = false;
        }
    }
}