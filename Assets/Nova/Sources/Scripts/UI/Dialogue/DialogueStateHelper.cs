using UnityEngine;

namespace Nova
{
    // Used by button ring
    public class DialogueStateHelper : MonoBehaviour
    {
        private DialogueState dialogueState;

        private void Awake()
        {
            dialogueState = Utils.FindNovaController().DialogueState;
        }

        public void StartNormal()
        {
            dialogueState.state = DialogueState.State.Normal;
        }

        public void StartAuto()
        {
            dialogueState.state = DialogueState.State.Auto;
        }

        public void StartFastForward()
        {
            dialogueState.state = DialogueState.State.FastForward;
        }
    }
}
