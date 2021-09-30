using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    public class SaveEntryController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Text idText;
        private NovaTextOutline idTextOutline;

        // private Text headerText;
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

        private readonly UnityEvent onPointerEnter = new UnityEvent();
        private readonly UnityEvent onPointerExit = new UnityEvent();

        private void Awake()
        {
            var container = transform.Find("Container");
            var header = container.Find("Header");
            idText = header.Find("Id").GetComponent<Text>();
            idTextOutline = header.Find("Id").GetComponent<NovaTextOutline>();
            // headerText = header.Find("Chapter").GetComponent<Text>();
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
            // headerText.gameObject.SetActive(false);
            dateText.gameObject.SetActive(false);

            deleteButtonEnabled = false;
            deleteButton.gameObject.SetActive(false);

            thumbnailImage.sprite = newThumbnailSprite == null ? defaultThumbnailSprite : newThumbnailSprite;

            InitButton(thumbnailButton, onThumbnailButtonClicked);
        }

        public void Init(string newIDText, string newHeaderText, string newDateText, bool isLatest,
            Sprite newThumbnailSprite,
            UnityAction onEditButtonClicked, UnityAction onDeleteButtonClicked,
            UnityAction onThumbnailButtonClicked, UnityAction onEnter, UnityAction onExit)
        {
            idText.text = newIDText;
            // headerText.gameObject.SetActive(true);
            // headerText.text = newHeaderText;
            dateText.gameObject.SetActive(true);
            dateText.text = newDateText;

            if (newThumbnailSprite == null)
            {
                latest.SetActive(false);
                deleteButtonEnabled = false;
                deleteButton.gameObject.SetActive(false);
                thumbnailImage.sprite = defaultThumbnailSprite;
            }
            else
            {
                latest.SetActive(isLatest);
                deleteButtonEnabled = true;
                thumbnailImage.sprite = newThumbnailSprite;
            }

            InitButton(deleteButton, onDeleteButtonClicked);
            InitButton(thumbnailButton, onThumbnailButtonClicked);

            onPointerEnter.RemoveAllListeners();
            if (onEnter != null)
            {
                onPointerEnter.AddListener(onEnter);
            }

            onPointerExit.RemoveAllListeners();
            if (onExit != null)
            {
                onPointerExit.AddListener(onExit);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (deleteButtonEnabled)
            {
                deleteButton.gameObject.SetActive(true);
            }

            onPointerEnter.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (deleteButtonEnabled)
            {
                deleteButton.gameObject.SetActive(false);
            }

            onPointerExit.Invoke();
        }

        // TODO: OnPointerExit does not work on Android?
        public void HideDeleteButton()
        {
            deleteButton.gameObject.SetActive(false);
        }
    }
}