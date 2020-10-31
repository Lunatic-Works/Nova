using System;
using UnityEngine.UI;

namespace Nova
{
    public class ConfigViewController : ViewControllerBase
    {
        public const string FirstShownKeySuffix = "FirstShown";

        public Button resetDefaultButton;
        public Button resetAlertsButton;
        public Button restoreButton;
        public Button backButton;
        public Button returnTitleButton;
        public Button exitGameButton;
        public InputMappingController inputMappingController;

        private ConfigManager configManager;
        private bool fromTitle;

        protected override void Awake()
        {
            base.Awake();

            returnTitleButton.onClick.AddListener(ReturnTitle);
            exitGameButton.onClick.AddListener(Utils.ExitWithConfirm);

            configManager = Utils.FindNovaGameController().ConfigManager;

            resetDefaultButton.onClick.AddListener(ResetDefault);
            resetAlertsButton.onClick.AddListener(ResetAlerts);
            if (restoreButton)
            {
                restoreButton.onClick.AddListener(configManager.Restore);
            }

            backButton.onClick.AddListener(Hide);
        }

        public override void Show(Action onFinish)
        {
            fromTitle = false;
            base.Show(() => { onFinish?.Invoke(); });
        }

        public void ShowFromTitle()
        {
            Show();
            fromTitle = true;
        }

        public override void Hide(Action onFinish)
        {
            configManager.Apply();
            base.Hide(onFinish);
        }

        private void _returnTitle()
        {
            NovaAnimation.StopAll();
            viewManager.titlePanel.SetActive(true);
            this.SwitchView<TitleController>();
        }

        private void ReturnTitle()
        {
            if (fromTitle)
            {
                _returnTitle();
            }
            else
            {
                Alert.Show(
                    null,
                    I18n.__("ingame.title.confirm"),
                    _returnTitle,
                    null,
                    "ReturnTitle"
                );
            }
        }

        private void ResetDefault()
        {
            Alert.Show(null, I18n.__("config.alert.resetdefault"), () =>
            {
                configManager.ResetToDefault();
                inputMappingController.ResetDefault();
                configManager.Apply();
                // error will occur if reset default button is clicked before the InputMappingController starts
                // as the InputMappingController will reload data from current InputMapping, which will not get
                // updated unless the apply method get called.
                inputMappingController.Apply();
            });
        }

        private void _resetAlerts()
        {
            foreach (string key in configManager.GetAllTrackedKeys())
            {
                if (key.StartsWith(Alert.AlertKeyPrefix))
                {
                    configManager.SetInt(key, 1);
                }

                if (key.EndsWith(FirstShownKeySuffix))
                {
                    configManager.SetInt(key, 0);
                }
            }
        }

        private void ResetAlerts()
        {
            Alert.Show(null, I18n.__("config.alert.resetalerts"), _resetAlerts);
        }

        // No alert for restore and apply
    }
}