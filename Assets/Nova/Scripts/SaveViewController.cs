// TODO
// Save/Load/Show thumbnail
// Resize/Clip screenshot, scale thumbnail by shorter axis (some code are in ScreenCapturer.cs)
// Show preview when mouse is hovering
// GC of Texture and Sprite
//
// Page 0 for auto save, last page for new page
// Auto hide edit and delete buttons
// UI to edit bookmark's description
//
// Function to update bookmark's description

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public enum SaveViewMode
    {
        Save,
        Load
    }

    public class SaveViewController : MonoBehaviour
    {
        public GameState gameState;
        public GameObject SaveEntryPrefab;
        public GameObject SaveEntryRowPrefab;
        public Sprite testThumbnailSprite;
        public int maxRow;
        public int maxCol;
        public bool canSave;

        private int maxSaveEntry;
        private int page = 1;

        // maxPage is updated when ShowPage is called
        // Use a data structure to maintain maximum for usedSaveSlots may improve performance
        private int maxPage = 1;

        private GameObject savePanel;
        private Image thumbnailImage;
        private Text thumbnailText;
        private Button saveButton;
        private Button loadButton;
        private Button leftButton;
        private Button rightButton;
        private Text leftButtonText;
        private Text rightButtonText;
        private Text pageText;

        private readonly List<GameObject> saveEntries = new List<GameObject>();
        private HashSet<int> usedSaveSlots;

        private SaveViewMode saveViewMode;

        private ScreenCapturer screenCapturer;
        // screenTexture and screenSprite are created when Show is called and savePanel is not active
        private Texture2D screenTexture;
        private Sprite screenSprite;

        private const string dateTimeFormat = "yyyy/MM/dd HH:mm";
        private string previewTextFormat;
        private string currentNodeName;
        private string currentDialogueText;

        private void Awake()
        {
            maxSaveEntry = maxRow * maxCol;

            savePanel = transform.Find("SavePanel").gameObject;
            thumbnailImage = savePanel.transform.Find("Background/Left/Thumbnail").GetComponent<Image>();
            thumbnailText = savePanel.transform.Find("Background/Left/TextBox/Text").GetComponent<Text>();
            var bottom = savePanel.transform.Find("Background/Right/Bottom").gameObject;
            saveButton = bottom.transform.Find("SaveButton").gameObject.GetComponent<Button>();
            loadButton = bottom.transform.Find("LoadButton").gameObject.GetComponent<Button>();
            var pager = bottom.transform.Find("Pager").gameObject;
            var leftButtonPanel = pager.transform.Find("LeftButton").gameObject;
            leftButton = leftButtonPanel.GetComponent<Button>();
            leftButtonText = leftButtonPanel.GetComponent<Text>();
            var rightButtonPanel = pager.transform.Find("RightButton").gameObject;
            rightButton = rightButtonPanel.GetComponent<Button>();
            rightButtonText = rightButtonPanel.GetComponent<Text>();
            pageText = pager.transform.Find("PageText").gameObject.GetComponent<Text>();

            if (canSave)
            {
                saveButton.onClick.AddListener(() => ShowSave());
            }
            else
            {
                // Cannot SetActive(false), otherwise layout will break
                saveButton.gameObject.GetComponent<CanvasGroup>().alpha = 0.0f;
            }
            loadButton.onClick.AddListener(() => ShowLoad());
            leftButton.onClick.AddListener(() => PageLeft());
            rightButton.onClick.AddListener(() => PageRight());

            var saveEntryGrid = savePanel.transform.Find("Background/Right/Top").gameObject;
            for (var rowIdx = 0; rowIdx < maxRow; ++rowIdx)
            {
                var saveEntryRow = Instantiate(SaveEntryRowPrefab);
                saveEntryRow.transform.SetParent(saveEntryGrid.transform);
                for (var colIdx = 0; colIdx < maxCol; ++colIdx)
                {
                    var saveEntry = Instantiate(SaveEntryPrefab);
                    saveEntry.transform.SetParent(saveEntryRow.transform);
                    saveEntries.Add(saveEntry);
                }
            }

            screenCapturer = gameObject.GetComponent<ScreenCapturer>();

            previewTextFormat = thumbnailText.text;
        }

        private void Start()
        {
            usedSaveSlots = gameState.checkpointManager.UsedSaveSlots;
            gameState.DialogueChanged.AddListener(OnDialogueChanged);
            ShowPage();
        }

        private void OnDialogueChanged(DialogueChangedEventData dialogueChangedEventData)
        {
            currentNodeName = dialogueChangedEventData.labelName;
            currentDialogueText = dialogueChangedEventData.text;
        }

        private Sprite TextureToSprite(Texture2D texture)
        {
            return Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
        }

        private void Show()
        {
            if (!savePanel.activeInHierarchy)
            {
                screenTexture = screenCapturer.GetTexture();
                screenSprite = TextureToSprite(screenTexture);
            }
            savePanel.SetActive(true);
            ShowPreview(screenSprite, string.Format(
                previewTextFormat,
                DateTime.Now.ToString(dateTimeFormat),
                currentNodeName,
                currentDialogueText
            ));
        }

        public void ShowSave()
        {
            Show();
            saveViewMode = SaveViewMode.Save;
            ShowPage();
        }

        public void ShowLoad()
        {
            Show();
            saveViewMode = SaveViewMode.Load;
            ShowPage();
        }

        public void Hide()
        {
            savePanel.SetActive(false);
        }

        private void PageLeft()
        {
            if (page > 1)
            {
                --page;
                ShowPage();
            }
        }

        private void PageRight()
        {
            if (page < maxPage)
            {
                ++page;
                ShowPage();
            }
        }

        private void SaveBookmark(int saveId)
        {
            var bookmark = gameState.GetBookmark();
            // bookmark.ScreenShot = screenTexture;
            gameState.checkpointManager.SaveBookmark(saveId, bookmark);
            Hide();
        }

        private void LoadBookmark(int saveId)
        {
            var bookmark = gameState.checkpointManager.LoadBookmark(saveId);
            Debug.Log(string.Format("Load bookmark, chapter {0}, index {1}",
                bookmark.NodeHistory.Last(), bookmark.DialogueIndex));
            gameState.LoadBookmark(bookmark);
            Hide();
        }

        private void EditBookmark(int saveId)
        {

        }

        private void DeleteBookmark(int saveId)
        {
            gameState.checkpointManager.DeleteBookmark(saveId);
            ShowPage();
        }

        private void ShowPreview(Sprite newSprite, string newText)
        {
            thumbnailImage.sprite = newSprite;
            thumbnailText.text = newText;
        }

        private void ShowPage()
        {
            if (usedSaveSlots.Any()){
                maxPage = (usedSaveSlots.Max() + maxSaveEntry - 1) / maxSaveEntry;
                // New page to save
                if (saveViewMode == SaveViewMode.Save)
                {
                    ++maxPage;
                }
            }
            else
            {
                maxPage = 1;
            }
            if (maxPage < page)
            {
                page = maxPage;
            }
            pageText.text = string.Format("{0} / {1}", page, maxPage);

            if (saveViewMode == SaveViewMode.Save)
            {
                saveButton.interactable = false;
                loadButton.interactable = true;

            }
            else // saveViewMode == SaveViewMode.Load
            {
                saveButton.interactable = true;
                loadButton.interactable = false;
            }

            for (var i = 0; i < maxSaveEntry; ++i)
            {
                int saveId = (page - 1) * maxSaveEntry + i + 1;
                string newIdText = "#" + saveId.ToString();

                // Load properties from bookmark
                string newHeaderText;
                string newFooterText;
                UnityAction onThumbnailButtonClicked;
                UnityAction onEditButtonClicked;
                UnityAction onDeleteButtonClicked;
                Sprite newThumbnailSprite;
                if (usedSaveSlots.Contains(saveId))
                {
                    Bookmark bookmark = gameState.checkpointManager[saveId];
                    newHeaderText = bookmark.NodeHistory.Last();
                    newFooterText = bookmark.CreationTime.ToString(dateTimeFormat);

                    if (saveViewMode == SaveViewMode.Save)
                    {
                        onThumbnailButtonClicked = () => SaveBookmark(saveId);
                        onEditButtonClicked = () => EditBookmark(saveId);
                        onDeleteButtonClicked = () => DeleteBookmark(saveId);
                    }
                    else // saveViewMode == SaveViewMode.Load
                    {
                        onThumbnailButtonClicked = () => LoadBookmark(saveId);
                        onEditButtonClicked = () => EditBookmark(saveId);
                        onDeleteButtonClicked = () => DeleteBookmark(saveId);
                    }

                    newThumbnailSprite = testThumbnailSprite;
                    // newThumbnailSprite = TextureToSprite(bookmark.ScreenShot);
                }
                else // Bookmark with this saveId is not found
                {
                    newHeaderText = "";
                    newFooterText = "";

                    if (saveViewMode == SaveViewMode.Save)
                    {
                        onThumbnailButtonClicked = () => SaveBookmark(saveId);
                        onEditButtonClicked = null;
                        onDeleteButtonClicked = null;
                    }
                    else // saveViewMode == SaveViewMode.Load
                    {
                        onThumbnailButtonClicked = null;
                        onEditButtonClicked = null;
                        onDeleteButtonClicked = null;
                    }

                    newThumbnailSprite = null;
                }

                // Update UI of saveEntry
                var saveEntry = saveEntries[i];
                var saveEntryController = saveEntry.GetComponent<SaveEntryController>();
                saveEntryController.Init(newIdText, newHeaderText, newFooterText,
                    onThumbnailButtonClicked, onEditButtonClicked, onDeleteButtonClicked,
                    newThumbnailSprite);
            }
        }
    }
}