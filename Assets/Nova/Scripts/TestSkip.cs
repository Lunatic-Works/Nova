using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class TestSkip : MonoBehaviour
    {
        public DialogueBoxController dialogueBoxController;

        void Start()
        {
            dialogueBoxController.SkipModeStarts.AddListener(OnSkipModeStarts);
            dialogueBoxController.SkipModeStops.AddListener(OnSkipModeEnds);
        }

        private void OnSkipModeStarts()
        {
            Debug.Log("Skip Start");
           // dialogueBoxController.State = DialogueBoxState.Normal;
        }

        private void OnSkipModeEnds()
        {
            Debug.Log("Skip End");
        }
    }
}