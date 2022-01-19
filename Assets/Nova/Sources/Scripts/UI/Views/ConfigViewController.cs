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
        public Button exitGameButton;
        public InputMappingController inputMappingController;

        private ConfigManager configManager;
        private bool fromTitle;

        protected override void Awake()
        {
            base.Awake();

            returnTitleButton.onClick.AddListener(ReturnTitle);
            exitGameButton.onClick.AddListener(Utils.QuitWithConfirm);

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
                inputMappingController.Apply();
                I18n.CurrentLocale = Application.systemLanguage;
            });
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
            Alert.Show(null, I18n.__("config.alert.resetalerts"), _resetAlerts);
        }

        // No alert for restore and apply

        protected override void OnActivatedUpdate()
        {
            // Avoid going back when recoding shortcuts
            if (inputMappingController.compoundKeyRecorder.gameObject.activeInHierarchy)
            {
                return;
            }

            base.OnActivatedUpdate();
        }
    }
}