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

            var test_bool = variables.Get<bool>("test_bool");
            var test_int = variables.Get<int>("test_int");
            var test_float = variables.Get<float>("test_float");
            var test_double = variables.Get<double>("test_double");
            Debug.Log($"{test_bool} {test_int} {test_float} {test_double}");
            variables.Set("test_bool", false);
            variables.Set("test_int", 321);
            variables.Set("test_float", 6.54f);
            variables.Set("test_double", 9.87);
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