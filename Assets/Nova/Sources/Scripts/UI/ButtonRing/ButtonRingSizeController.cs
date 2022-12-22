using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control button ring size based on the value in ConfigManager
    /// </summary>
    public class ButtonRingSizeController : MonoBehaviour
    {
        public string configKeyName;

        private ButtonRing buttonRing;
        private ConfigManager configManager;

        private void Awake()
        {
            buttonRing = GetComponentInChildren<ButtonRing>();
            configManager = Utils.FindNovaController().ConfigManager;
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
            buttonRing.sectorRadius = configManager.GetFloat(configKeyName);
        }
    }
}
