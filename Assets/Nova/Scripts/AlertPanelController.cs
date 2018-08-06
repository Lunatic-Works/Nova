using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class AlertPanelController : MonoBehaviour
    {
        private Text _title;
        private Text _bodyContent;
        private Button _confirmButton;
        private Button _cancelButton;
        private GameObject _ignorePanel;
        private Toggle _ignoreToggle;

        private void Awake()
        {
            _title = transform.Find("Background/Title").GetComponent<Text>();
            _bodyContent = transform.Find("Background/Text").GetComponent<Text>();
            _confirmButton = transform.Find("Background/Buttons/Confirm").GetComponent<Button>();
            _cancelButton = transform.Find("Background/Buttons/Cancel").GetComponent<Button>();
            _ignorePanel = transform.Find("Background/Ignore").gameObject;
            _ignoreToggle = _ignorePanel.transform.Find("Toggle").GetComponent<Toggle>();
        }

        public void Init(string title, string bodyContent,
            UnityAction onClickConfirm = null, UnityAction onClickCancel = null,
            Wrap<bool> ignore = null)
        {
            if (title == null)
            {
                _title.gameObject.SetActive(false);
            }
            else
            {
                _title.text = title;
            }

            if (bodyContent == null)
            {
                _bodyContent.gameObject.SetActive(false);
            }
            else
            {
                _bodyContent.text = bodyContent;
            }

            if (onClickConfirm != null)
            {
                _confirmButton.onClick.AddListener(onClickConfirm);
            }

            if (onClickCancel != null)
            {
                _cancelButton.onClick.AddListener(onClickCancel);
            }

            if (ignore == null)
            {
                _ignorePanel.SetActive(false);
            }
            else
            {
                _ignoreToggle.onValueChanged.AddListener((value) => { ignore.value = value; });
            }

            // Destory alert panel when button clicked
            _confirmButton.onClick.AddListener(DestroyMe);
            _cancelButton.onClick.AddListener(DestroyMe);
        }

        private void DestroyMe()
        {
            Destroy(gameObject);
        }
    }
}