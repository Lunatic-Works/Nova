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

        public int index { get; private set; }
        public CompoundKey key { get; private set; }

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
            label.text = key.ToString();
        }

        public void Init(InputMappingController controller, int index)
        {
            this.controller = controller;
            this.index = index;
            key = this.controller.currentCompoundKeys[index];
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