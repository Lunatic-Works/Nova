using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class SaveEntryController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Text idText;
        private TextOutline idTextOutline;
        private Text dateText;

        private GameObject latest;
        private Button thumbnailButton;
        private Image thumbnailImage;
        private Sprite defaultThumbnailSprite;
        private Button deleteButton;

        private Color saveTextColor;
        private Color saveTextOutlineColor;
        private Color loadTextColor;
        private Color loadTextOutlineColor;

        private bool deleteButtonEnabled;

        private UnityAction<PointerEventData> onPointerEnter;
        private UnityAction<PointerEventData> onPointerExit;

        private void Awake()
        {
            var container = transform.Find("Container");
            var header = container.Find("Header");
            idText = header.Find("Id").GetComponent<Text>();
            idTextOutline = header.Find("Id").GetComponent<TextOutline>();
            dateText = header.Find("Date").GetComponent<Text>();
            latest = container.Find("Latest").gameObject;
            thumbnailButton = container.GetComponent<Button>();
            thumbnailImage = container.Find("Image").GetComponent<Image>();
            defaultThumbnailSprite = thumbnailImage.sprite;
            deleteButton = transform.Find("DeleteButton").GetComponent<Button>();

            ColorUtility.TryParseHtmlString("#33FF33FF", out saveTextColor);
            ColorUtility.TryParseHtmlString("#66FF6643", out saveTextOutlineColor);
            ColorUtility.TryParseHtmlString("#FF3333FF", out loadTextColor);
            ColorUtility.TryParseHtmlString("#FF666643", out loadTextOutlineColor);
        }

        public SaveViewMode mode
        {
            set
            {
                idText.color = value == SaveViewMode.Save ? saveTextColor : loadTextColor;
                idTextOutline.effectColor = value == SaveViewMode.Save ? saveTextOutlineColor : loadTextOutlineColor;
            }
        }

        private static void InitButton(Button button, UnityAction onClick)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
                button.interactable = true;
            }
            else
            {
                button.interactable = false;
            }
        }

        public void InitAsPreview(Sprite newThumbnailSprite, UnityAction onThumbnailButtonClicked)
        {
            idText.text = "--";
            dateText.gameObject.SetActive(false);

            latest.SetActive(false);
            deleteButtonEnabled = false;
            HideDeleteButton();
            thumbnailImage.sprite = newThumbnailSprite == null ? defaultThumbnailSprite : newThumbnailSprite;

            InitButton(deleteButton, null);
            InitButton(thumbnailButton, onThumbnailButtonClicked);
            onPointerEnter = null;
            onPointerExit = null;
        }

        public void Init(string newIDText, string newDateText, bool isLatest, Sprite newThumbnailSprite,
            UnityAction onDeleteButtonClicked, UnityAction onThumbnailButtonClicked,
            UnityAction<PointerEventData> onThumbnailButtonEnter, UnityAction<PointerEventData> onThumbnailButtonExit)
        {
            idText.text = newIDText;
            dateText.gameObject.SetActive(true);
            dateText.text = newDateText;

            if (newThumbnailSprite == null)
            {
                latest.SetActive(false);
                deleteButtonEnabled = false;
                HideDeleteButton();
                thumbnailImage.sprite = defaultThumbnailSprite;
            }
            else
            {
                latest.SetActive(isLatest);
                deleteButtonEnabled = true;
                HideDeleteButton();
                thumbnailImage.sprite = newThumbnailSprite;
            }

            InitButton(deleteButton, onDeleteButtonClicked);
            InitButton(thumbnailButton, onThumbnailButtonClicked);
            onPointerEnter = onThumbnailButtonEnter;
            onPointerExit = onThumbnailButtonExit;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointerEnter?.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onPointerExit?.Invoke(eventData);
        }

        public void ShowDeleteButton()
        {
            if (deleteButtonEnabled)
            {
                deleteButton.gameObject.SetActive(true);
            }
        }

        public void HideDeleteButton()
        {
            deleteButton.gameObject.SetActive(false);
        }
    }
}
