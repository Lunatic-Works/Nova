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

        private ConfigManager configManager;
        private DialogueBoxController dialogueBoxController;

        private void Awake()
        {
            configManager = Utils.FindNovaGameController().ConfigManager;
            dialogueBoxController = GetComponent<DialogueBoxController>();
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