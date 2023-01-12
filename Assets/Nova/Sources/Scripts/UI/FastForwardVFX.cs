using UnityEngine;

namespace Nova
{
    public class FastForwardVFX : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private float vfxDurationOnRestore = 0.2f;

        private GameState gameState;
        private DialogueState dialogueState;
        private NovaAnimation uiAnimation;
        private PostProcessing postProcessing;

        private int _count;

        private int count
        {
            get => _count;
            set
            {
                if (_count == 0)
                {
                    if (value > 0)
                    {
                        postProcessing.SetLayer(0, material);
                    }
                }
                else
                {
                    if (value == 0)
                    {
                        postProcessing.ClearLayer(0);
                    }
                }

                _count = value;
            }
        }

        private void Start()
        {
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;
            uiAnimation = Utils.FindViewManager().uiAnimation;
            postProcessing = UICameraHelper.Active.GetComponent<PostProcessing>();

            gameState.restoreStarts.AddListener(OnRestoreStarts);
            dialogueState.fastForwardModeStarts.AddListener(AddVFX);
            dialogueState.fastForwardModeStops.AddListener(RemoveVFX);
        }

        private void OnDestroy()
        {
            gameState.restoreStarts.RemoveListener(OnRestoreStarts);
            dialogueState.fastForwardModeStarts.RemoveListener(AddVFX);
            dialogueState.fastForwardModeStops.RemoveListener(RemoveVFX);
        }

        private void OnRestoreStarts()
        {
            AddVFX();
            uiAnimation.Do(null, vfxDurationOnRestore).Then(new ActionAnimationProperty(RemoveVFX));
        }

        private void AddVFX()
        {
            ++count;
        }

        private void RemoveVFX()
        {
            --count;
        }
    }
}
