using System.Collections;
using UnityEngine.UI;

namespace Nova
{
    public class AlertController : ViewControllerBase
    {
        private Text titleText;
        private Text bodyContentText;
        private Button confirmButton;
        private Button cancelButton;
        private Toggle ignoreToggle;

        private ConfigManager configManager;
        private AlertParameters param;

        protected override void Awake()
        {
            base.Awake();

            titleText = myPanel.transform.Find("Background/Title").GetComponent<Text>();
            bodyContentText = myPanel.transform.Find("Background/Text").GetComponent<Text>();
            confirmButton = myPanel.transform.Find("Background/Buttons/Confirm").GetComponent<Button>();
            cancelButton = myPanel.transform.Find("Background/Buttons/Cancel").GetComponent<Button>();
            ignoreToggle = myPanel.transform.Find("Background/Ignore").GetComponent<Toggle>();

            configManager = Utils.FindNovaGameController().ConfigManager;
        }

        public void Alert(AlertParameters param)
        {
            if (param.lite)
            {
                return;
            }

            StartCoroutine(DoAlert(param));
        }

        private IEnumerator DoAlert(AlertParameters param)
        {
            yield return 3;

            this.param = param;

            if (param.ignoreKey != "" && configManager.GetInt(param.ignoreKey) == 0)
            {
                param.onConfirm?.Invoke();
                yield break;
            }

            titleText.gameObject.SetActive(param.title != null);
            titleText.text = param.title;
            bodyContentText.gameObject.SetActive(param.bodyContent != null);
            bodyContentText.text = param.bodyContent;
            cancelButton.gameObject.SetActive(param.onConfirm != null || param.onCancel != null);
            ignoreToggle.gameObject.SetActive(param.ignoreKey != "");

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => Hide(param.onConfirm));

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() =>
            {
                if (param.ignoreKey != "")
                {
                    configManager.SetInt(param.ignoreKey, 1);
                }

                Hide(param.onCancel);
            });

            ignoreToggle.onValueChanged.RemoveAllListeners();
            ignoreToggle.isOn = false;
            if (param.ignoreKey != "")
            {
                // 0: ignore alert, 1: show alert
                ignoreToggle.onValueChanged.AddListener(value =>
                {
                    configManager.SetInt(param.ignoreKey, value ? 0 : 1);
                    configManager.Apply();
                });
            }

            Show();
        }

        protected override void BackHide()
        {
            if (param != null)
            {
                if (param.ignoreKey != "")
                {
                    configManager.SetInt(param.ignoreKey, 1);
                }

                Hide(param.onCancel);
            }
        }

        protected override void Update()
        {
            if (viewManager.currentView == CurrentViewType.Alert)
            {
                OnActivatedUpdate();
            }
        }
    }
}