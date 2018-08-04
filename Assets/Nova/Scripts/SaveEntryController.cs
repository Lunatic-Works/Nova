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
        private Image thumbnailImage;
        private Sprite defaultThumbnailSprite;

        private void Awake()
        {
            var header = transform.Find("Header").gameObject;
            idText = header.transform.Find("Id").gameObject.GetComponent<Text>();
            headerText = header.transform.Find("Text").gameObject.GetComponent<Text>();
            footerText = transform.Find("Footer/Text").gameObject.GetComponent<Text>();
            thumbnailButton = transform.Find("Thumbnail").gameObject.GetComponent<Button>();
            editButton = header.transform.Find("EditButton").gameObject.GetComponent<Button>();
            deleteButton = header.transform.Find("DeleteButton").gameObject.GetComponent<Button>();
            thumbnailImage = transform.Find("Thumbnail").gameObject.GetComponent<Image>();
            defaultThumbnailSprite = thumbnailImage.sprite;
        }

        private void InitButton(Button button, UnityAction onClickAction, bool hideButton = true)
        {
            if (onClickAction == null)
            {
                if (hideButton)
                {
                    button.gameObject.SetActive(false);
                }
                else
                {
                    button.gameObject.SetActive(true);
                    button.interactable = false;
                    button.onClick.RemoveAllListeners();
                }
                return;
            }
            else
            {
                button.gameObject.SetActive(true);
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClickAction);
            }
        }

        public void Init(string newIdText, string newHeaderText, string newFooterText,
            UnityAction onThumbnailButtonClicked, UnityAction onEditButtonClicked, UnityAction onDeleteButtonClicked,
            Sprite newThumbnailSprite)
        {
            idText.text = newIdText;
            headerText.text = newHeaderText;
            footerText.text = newFooterText;
            InitButton(thumbnailButton, onThumbnailButtonClicked, hideButton: false);
            InitButton(editButton, onEditButtonClicked);
            InitButton(deleteButton, onDeleteButtonClicked);

            if (newThumbnailSprite == null)
            {
                thumbnailImage.sprite = defaultThumbnailSprite;
            }
            else
            {
                thumbnailImage.sprite = newThumbnailSprite;
            }
        }
    }
}