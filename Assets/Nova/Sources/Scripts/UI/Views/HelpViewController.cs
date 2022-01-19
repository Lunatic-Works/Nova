using UnityEngine.UI;

namespace Nova
{
    public class HelpViewController : ViewControllerBase
    {
        public Button returnButton;
        public Button returnButton2;

        private const string GameFirstShownKey = ConfigManager.FirstShownKeyPrefix + "Game";

        private ConfigManager configManager;

        protected override void Awake()
        {
            base.Awake();

            returnButton.onClick.AddListener(Hide);
            returnButton2.onClick.AddListener(Hide);

            configManager = Utils.FindNovaGameController().ConfigManager;
        }

        protected override void Start()
        {
            base.Start();

            if (configManager.GetInt(GameFirstShownKey) == 0)
            {
                configManager.SetInt(GameFirstShownKey, 1);
                Show();
            }
        }
    }
}