using UnityEngine;

namespace Nova
{
    public class FastForwardVFX : MonoBehaviour
    {
        [SerializeField] private Material material;

        private DialogueState dialogueState;
        private PostProcessing postProcessing;

        private void Awake()
        {
            dialogueState = Utils.FindNovaController().DialogueState;
            postProcessing = UICameraHelper.Active.GetComponent<PostProcessing>();

            dialogueState.fastForwardModeStarts.AddListener(OnFastForwardModeStarts);
            dialogueState.fastForwardModeStops.AddListener(OnFastForwardModeStops);
        }

        private void OnDestroy()
        {
            dialogueState.fastForwardModeStarts.RemoveListener(OnFastForwardModeStarts);
            dialogueState.fastForwardModeStops.RemoveListener(OnFastForwardModeStops);
        }

        private void OnFastForwardModeStarts()
        {
            postProcessing.SetLayer(0, material);
        }

        private void OnFastForwardModeStops()
        {
            postProcessing.ClearLayer(0);
        }
    }
}
