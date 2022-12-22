using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ConfigViewController : ViewControllerBase
    {
        [SerializeField] private Button resetDefaultButton;
        [SerializeField] private Button resetAlertsButton;
        [SerializeField] private Button restoreButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button returnTitleButton;
        [SerializeField] private Button quitGameButton;
        [SerializeField] private InputMappingController inputMappingController;

        private ConfigManager configManager;
        private bool fromTitle;

        protected override void Awake()
        {
            base.Awake();

            returnTitleButton.onClick.AddListener(ReturnTitleWithAlert);
            quitGameButton.onClick.AddListener(Utils.QuitWithAlert);

            configManager = Utils.FindNovaController().ConfigManager;

            resetDefaultButton.onClick.AddListener(ResetDefaultWithAlert);
            resetAlertsButton.onClick.AddListener(ResetAlertsWithAlert);
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

        public void ReturnTitleWithCallback(Action onFinish)
        {
            NovaAnimation.StopAll();

            // TODO: Better transition between any two views
            viewManager.titlePanel.SetActive(true);

            this.SwitchView<TitleController>(onFinish);
        }

        private void ReturnTitle()
        {
            ReturnTitleWithCallback(null);
        }

        private void ReturnTitleWithAlert()
        {
            if (fromTitle)
            {
                ReturnTitle();
            }
            else
            {
                Alert.Show(null, "ingame.title.confirm", ReturnTitle, null, "ReturnTitle");
            }
        }

        private void ResetDefault()
        {
            configManager.ResetDefault();
            configManager.Apply();
            inputMappingController.ResetDefault();
            inputMappingController.Apply();
            I18n.CurrentLocale = Application.systemLanguage;
        }

        private void ResetDefaultWithAlert()
        {
            Alert.Show(null, "config.alert.resetdefault", ResetDefault);
        }

        private void ResetAlerts()
        {
            foreach (string key in configManager.GetAllTrackedKeys())
            {
                if (key.StartsWith(Alert.AlertKeyPrefix, StringComparison.Ordinal))
                {
                    configManager.SetInt(key, 1);
                }
                else if (key.StartsWith(ConfigManager.FirstShownKeyPrefix, StringComparison.Ordinal))
                {
                    configManager.SetInt(key, 0);
                }
            }
        }

        private void ResetAlertsWithAlert()
        {
            Alert.Show(null, "config.alert.resetalerts", ResetAlerts);
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
