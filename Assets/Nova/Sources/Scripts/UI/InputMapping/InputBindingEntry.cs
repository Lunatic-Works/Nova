using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class InputBindingEntry : MonoBehaviour
    {
        public Text label;
        public Color defaultColor;
        public Color activeColor;

        private InputMappingController controller;

        public CompositeInputBinding compositeBinding { get; private set; }

        private void Awake()
        {
            label.color = defaultColor;
        }

        public void Init(InputMappingController controller, CompositeInputBinding compositeBinding)
        {
            this.controller = controller;
            this.compositeBinding = compositeBinding;
            label.text = compositeBinding.ToString();
        }

        public void Remove()
        {
            controller.RemoveBinding(compositeBinding);
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
