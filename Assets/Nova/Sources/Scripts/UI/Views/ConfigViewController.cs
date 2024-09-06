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
        [SerializeField] private Material blurMaterial;

        private ConfigManager configManager;
        private bool fromTitle;
        private Texture2D screenTexture;

        protected override void Awake()
        {
            base.Awake();

            configManager = Utils.FindNovaController().ConfigManager;

            resetDefaultButton.onClick.AddListener(ResetDefaultWithAlert);
            resetAlertsButton.onClick.AddListener(ResetAlertsWithAlert);
            if (restoreButton)
            {
                restoreButton.onClick.AddListener(configManager.Restore);
            }

            backButton.onClick.AddListener(Hide);
            returnTitleButton.onClick.AddListener(ReturnTitleWithAlert);
            quitGameButton.onClick.AddListener(Utils.QuitWithAlert);
        }

        public override void Show(bool doTransition, Action onFinish)
        {
            if (!fromTitle)
            {
                if (screenTexture != null)
                {
                    Destroy(screenTexture);
                }

                screenTexture = ScreenCapturer.GetBookmarkThumbnailTexture(blurMaterial);
            }

            returnTitleButton.gameObject.SetActive(!fromTitle);

            base.Show(doTransition, onFinish);
        }

        public void ShowFromGame()
        {
            fromTitle = false;
            Show();
        }

        public void ShowFromTitle()
        {
            fromTitle = true;
            Show();
        }

        public override void Hide(bool doTransition, Action onFinish)
        {
            if (screenTexture != null)
            {
                Destroy(screenTexture);
            }

            configManager.Flush();
            base.Hide(doTransition, onFinish);
        }

        public void ReturnTitleWithCallback(Action onFinish)
        {
            AutoSaveBookmark.Current.TrySave(screenTexture);
            viewManager.GetController<GameViewController>().HideImmediate();
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
            configManager.Flush();
            inputMappingController.ResetDefault();
            inputMappingController.Flush();
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
            if (inputMappingController.inputManager.isRebinding)
            {
                return;
            }

            base.OnActivatedUpdate();
        }
    }
}
