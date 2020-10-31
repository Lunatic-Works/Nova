using UnityEngine;

namespace Nova
{
    [RequireComponent(typeof(DialogueBoxController))]
    public class DialogueViewHelper : MonoBehaviour
    {
        private DialogueBoxController controller;

        private void Awake()
        {
            controller = GetComponent<DialogueBoxController>();
        }

        public void StartAuto()
        {
            controller.state = DialogueBoxState.Auto;
        }

        public void StartSkip()
        {
            controller.state = DialogueBoxState.Skip;
        }

        public void StartNormal()
        {
            controller.state = DialogueBoxState.Normal;
        }
    }
}