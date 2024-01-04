using UnityEngine;

namespace Nova
{
    public class SleepTimeoutManager : MonoBehaviour
    {
        [SerializeField] private VideoController videoController;

        private DialogueState dialogueState;
        private bool canSleep = true;

        private void Awake()
        {
            dialogueState = Utils.FindNovaController().DialogueState;
        }

        private void Update()
        {
            bool newCanSleep = dialogueState.isNormal && !videoController.isPlaying;
            if (newCanSleep != canSleep)
            {
                canSleep = newCanSleep;
                Screen.sleepTimeout = canSleep ? SleepTimeout.SystemSetting : SleepTimeout.NeverSleep;
            }
        }
    }
}
