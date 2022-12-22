using UnityEngine;

namespace Nova
{
    public enum RightButtonAction
    {
        // TODO: i18n
        [EnumDisplayName("显示环形菜单")] ShowButtonRing,
        [EnumDisplayName("隐藏对话框")] HideDialoguePanel,
        [EnumDisplayName("无操作")] None
    }

    /// <summary>
    /// Change the action of right button for DialogueBoxController
    /// </summary>
    [RequireComponent(typeof(GameViewInput))]
    public class RightButtonActionReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private ConfigManager configManager;
        private GameViewInput gameViewInput;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            gameViewInput = GetComponent<GameViewInput>();
        }

        private void OnEnable()
        {
            configManager.AddValueChangeListener(configKeyName, UpdateValue);
            UpdateValue();
        }

        private void OnDisable()
        {
            configManager.RemoveValueChangeListener(configKeyName, UpdateValue);
        }

        private void UpdateValue()
        {
            gameViewInput.rightButtonAction = (RightButtonAction)configManager.GetInt(configKeyName);
        }
    }
}
