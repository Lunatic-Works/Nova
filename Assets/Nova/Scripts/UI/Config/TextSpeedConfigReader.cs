using UnityEngine;

namespace Nova
{
    public class TextSpeedConfigReader : MonoBehaviour
    {
        public string configKey = "TextSpeed";

        private ConfigManager _configManager;

        private ConfigManager configManager
        {
            get
            {
                if (_configManager == null)
                {
                    _configManager = Utils.FindNovaGameController().ConfigManager;
                }

                return _configManager;
            }
        }

        public float perCharacterFadeInDuration =>
            Mathf.Max(2.0f * Mathf.Pow(0.1f, configManager.GetFloat(configKey)) - 0.02f, 0.001f);
    }
}