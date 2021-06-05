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
    [RequireComponent(typeof(DialogueBoxController))]
    public class RightButtonActionController : MonoBehaviour
    {
        public string configKeyName;

        private DialogueBoxController dialogueBoxController;
        private ConfigManager configManager;

        private void Awake()
        {
            dialogueBoxController = GetComponent<DialogueBoxController>();
            configManager = Utils.FindNovaGameController().ConfigManager;
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
            dialogueBoxController.rightButtonAction = (RightButtonAction)configManager.GetInt(configKeyName);
        }
    }
}