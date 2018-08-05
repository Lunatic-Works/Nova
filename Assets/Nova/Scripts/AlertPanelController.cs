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

        private void Awake()
        {
            _title = transform.Find("AlertBackground/Title").GetComponent<Text>();
            _bodyContent = transform.Find("AlertBackground/Body").GetComponent<Text>();
            _confirmButton = transform.Find("AlertBackground/Buttons/Confirm").GetComponent<Button>();
            _cancelButton = transform.Find("AlertBackground/Buttons/Cancel").GetComponent<Button>();
        }

        public void Init(string title, string bodyContent,
            UnityAction onClickConfirm = null, UnityAction onClickCancel = null)
        {
            _title.text = title;
            _bodyContent.text = bodyContent;

            if (onClickConfirm != null)
            {
                _confirmButton.onClick.AddListener(onClickConfirm);
            }

            if (onClickCancel != null)
            {
                _cancelButton.onClick.AddListener(onClickCancel);
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