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
            button.onClick.RemoveAllListeners();
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
                }
            }
            else
            {
                button.gameObject.SetActive(true);
                button.interactable = true;
                button.onClick.AddListener(onClickAction);
            }
        }

        public void Init(string newIdText, string newHeaderText, string newFooterText, Sprite newThumbnailSprite,
            UnityAction onEditButtonClicked, UnityAction onDeleteButtonClicked,
            UnityAction onThumbnailButtonClicked, UnityAction onThumbnailButtonEnter, UnityAction onThumbnailButtonExit)
        {
            idText.text = newIdText;
            headerText.text = newHeaderText;
            footerText.text = newFooterText;

            if (newThumbnailSprite == null)
            {
                thumbnailImage.sprite = defaultThumbnailSprite;
            }
            else
            {
                thumbnailImage.sprite = newThumbnailSprite;
            }

            InitButton(editButton, onEditButtonClicked);
            InitButton(deleteButton, onDeleteButtonClicked);

            InitButton(thumbnailButton, onThumbnailButtonClicked, hideButton: false);
            var thumbnailButtonEnterExit = thumbnailButton.gameObject.GetComponent<PointerEnterExit>();
            thumbnailButtonEnterExit.onPointerEnter.RemoveAllListeners();
            if (onThumbnailButtonEnter != null)
            {
                thumbnailButtonEnterExit.onPointerEnter.AddListener(onThumbnailButtonEnter);
            }
            thumbnailButtonEnterExit.onPointerExit.RemoveAllListeners();
            if (onThumbnailButtonExit != null)
            {
                thumbnailButtonEnterExit.onPointerExit.AddListener(onThumbnailButtonExit);
            }
        }
    }
}