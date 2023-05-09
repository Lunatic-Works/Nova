using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class InputFieldMinigameController : MonoBehaviour
    {
        public TMP_InputField inputField;
        public Button closeButton;

        private GameState gameState;
        private Variables variables;

        private void Awake()
        {
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            variables = gameState.variables;
            closeButton.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            variables.Set("v_input_tmp", inputField.text);
            gameState.SignalFence(true);
        }
    }
}
