using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ExampleMinigameController : MonoBehaviour
    {
        public Text text;
        public InputField inputField;
        public Button closeButton;

        private GameState gameState;
        private Variables variables;
        private CheckpointHelper checkpointHelper;

        private void Awake()
        {
            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            variables = gameState.variables;
            checkpointHelper = controller.CheckpointHelper;

            var choice = variables.Get<string>("choice");
            var count = checkpointHelper.GetGlobalVariable<int>("minigame_count", 0);
            count += 1;
            checkpointHelper.SetGlobalVariable("minigame_count", count);
            text.text = string.Format(text.text, choice, count);

            closeButton.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            variables.Set("minigame_text", inputField.text);
            gameState.SignalFence(true);
        }
    }
}