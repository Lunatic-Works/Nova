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

        public void StartFastForward()
        {
            controller.state = DialogueBoxState.FastForward;
        }

        public void StartNormal()
        {
            controller.state = DialogueBoxState.Normal;
        }
    }
}