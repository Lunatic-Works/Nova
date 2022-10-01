using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ConfigViewController : ViewControllerBase
    {
        public Button resetDefaultButton;
        public Button resetAlertsButton;
        public Button restoreButton;
        public Button backButton;
        public Button returnTitleButton;
        public Button quitGameButton;
        public InputMappingController inputMappingController;

        private ConfigManager configManager;
        private bool fromTitle;

        protected override void Awake()
        {
            base.Awake();

            returnTitleButton.onClick.AddListener(ReturnTitle);
            quitGameButton.onClick.AddListener(Utils.QuitWithConfirm);

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
            base.Show(onFinish);
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

            // TODO: Better transition between any two views
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
                Alert.Show(null, "ingame.title.confirm", _returnTitle, null, "ReturnTitle");
            }
        }

        private void _resetDefault()
        {
            configManager.ResetDefault();
            configManager.Apply();
            inputMappingController.ResetDefault();
            inputMappingController.Apply();
            I18n.CurrentLocale = Application.systemLanguage;
        }

        private void ResetDefault()
        {
            Alert.Show(null, "config.alert.resetdefault", _resetDefault);
        }

        private void _resetAlerts()
        {
            foreach (string key in configManager.GetAllTrackedKeys())
            {
                if (key.StartsWith(Alert.AlertKeyPrefix, StringComparison.Ordinal))
                {
                    configManager.SetInt(key, 1);
                }

                if (key.StartsWith(ConfigManager.FirstShownKeyPrefix, StringComparison.Ordinal))
                {
                    configManager.SetInt(key, 0);
                }
            }
        }

        private void ResetAlerts()
        {
            Alert.Show(null, "config.alert.resetalerts", _resetAlerts);
        }

        // No alert for restore and apply

        protected override void OnActivatedUpdate()
        {
            // Avoid going back when recoding shortcuts
            if (inputMappingController.compoundKeyRecorder.isRebinding)
            {
                return;
            }

            base.OnActivatedUpdate();
        }
    }
}
