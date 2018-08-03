using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class SaveEntryController : MonoBehaviour
    {
        private Text idText;
        private Text headerText;
        private Text footerText;
        private Button thumbnailButton;
        private Button editButton;
        private Button deleteButton;

        private void Awake()
        {
            var header = transform.Find("Header").gameObject;
            idText = header.transform.Find("Id").gameObject.GetComponent<Text>();
            headerText = header.transform.Find("Text").gameObject.GetComponent<Text>();
            footerText = transform.Find("Footer/Text").gameObject.GetComponent<Text>();
            thumbnailButton = transform.Find("Thumbnail").gameObject.GetComponent<Button>();
            editButton = header.transform.Find("EditButton").gameObject.GetComponent<Button>();
            deleteButton = header.transform.Find("DeleteButton").gameObject.GetComponent<Button>();
        }

        private void InitButton(Button button, UnityAction onClickAction)
        {
            if (onClickAction == null)
            {
                button.gameObject.SetActive(false);
                return;
            }
            else
            {
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClickAction);
            }
        }

        public void Init(string newIdText, string newHeaderText, string newFooterText,
            UnityAction onThumbnailButtonClicked, UnityAction onEditButtonClicked, UnityAction onDeleteButtonClicked)
        {
            idText.text = newIdText;
            headerText.text = newHeaderText;
            footerText.text = newFooterText;
            InitButton(thumbnailButton, onThumbnailButtonClicked);
            InitButton(editButton, onEditButtonClicked);
            InitButton(deleteButton, onDeleteButtonClicked);
        }
    }
}