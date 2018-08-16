// TODO
// Scale thumbnail by shorter axis
// Page 0 for quick save, -1 for auto save, last page for new page
// UI to edit bookmark's description
// Compress thumbnail
//
// Function to update bookmark's description

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public enum SaveViewMode
    {
        Save,
        Load
    }

    public class BookmarkSaveEventData
    {
        public BookmarkSaveEventData(int saveId, Bookmark bookmark)
        {
            this.saveId = saveId;
            this.bookmark = bookmark;
        }

        public int saveId { get; private set; }
        public Bookmark bookmark { get; private set; }
    }

    [System.Serializable]
    public class BookmarkSaveEvent : UnityEvent<BookmarkSaveEventData>
    {
    }

    public class BookmarkLoadEventData
    {
        public BookmarkLoadEventData(Bookmark bookmark)
        {
            this.bookmark = bookmark;
        }

        public Bookmark bookmark { get; private set; }
    }

    [System.Serializable]
    public class BookmarkLoadEvent : UnityEvent<BookmarkLoadEventData>
    {
    }

    public class BookmarkDeleteEventData
    {
        public BookmarkDeleteEventData(int saveId)
        {
            this.saveId = saveId;
        }

        public int saveId { get; private set; }
    }

    [System.Serializable]
    public class BookmarkDeleteEvent : UnityEvent<BookmarkDeleteEventData>
    {
    }

    public class SaveViewController : MonoBehaviour
    {
        public GameObject SaveEntryPrefab;
        public GameObject SaveEntryRowPrefab;
        public int maxRow;
        public int maxCol;
        public bool canSave;

        public BookmarkSaveEvent BookmarkSave;
        public BookmarkLoadEvent BookmarkLoad;
        public BookmarkDeleteEvent BookmarkDelete;
        
        private GameState gameState;
        private CheckpointManager checkpointManager;

        private GameObject savePanel;
        private Button backgroundButton;
        private Image thumbnailImage;
        private Text thumbnailText;
        private Button saveButton;
        private Button loadButton;
        private Button leftButton;
        private Button rightButton;
        // private Text leftButtonText;
        // private Text rightButtonText;
        private Text pageText;
        private Sprite defaultThumbnailSprite;

        private readonly List<GameObject> saveEntries = new List<GameObject>();
        private readonly Dictionary<int, Sprite> _cachedThumbnailSprite = new Dictionary<int, Sprite>();

        private int maxSaveEntry;
        private int page = 1;

        // maxPage is updated when ShowPage is called
        private int maxPage = 1;

        // selectedSaveId == -1 means no bookmark is selected
        private int _selectedSaveId;

        private int selectedSaveId
        {
            get { return _selectedSaveId; }

            set
            {
                Assert.IsTrue(checkpointManager.SaveSlotsMetadata.ContainsKey(value) || value == -1,
                    "Nova: selectedSaveId must be a saveId with existing bookmark, or -1");
                _selectedSaveId = value;
                if (value == -1)
                {
                    ShowPreviewScreen();
                }
                else
                {
                    ShowPreviewBookmark(value);
                }
            }
        }

        private SaveViewMode saveViewMode = SaveViewMode.Save;
        private BookmarkType saveViewBookmarkType = BookmarkType.NormalSave;

        private ScreenCapturer screenCapturer;

        // screenTexture and screenSprite are created when Show is called and savePanel is not active
        // They are destroyed when Hide is called and savePanel is active
        private Texture2D screenTexture;
        private Sprite screenSprite;

        private const string dateTimeFormat = "yyyy/MM/dd HH:mm";
        private string currentNodeName;
        private string currentDialogueText;

        private AlertController alertController;

        private void Awake()
        {
            maxSaveEntry = maxRow * maxCol;

            gameState = Utils.FindNovaGameController().GetComponent<GameState>();
            checkpointManager = Utils.FindNovaGameController().GetComponent<CheckpointManager>();

            savePanel = transform.Find("SavePanel").gameObject;
            backgroundButton = savePanel.transform.Find("Background").GetComponent<Button>();
            thumbnailImage = savePanel.transform.Find("Background/Left/Thumbnail").GetComponent<Image>();
            defaultThumbnailSprite = thumbnailImage.sprite;
            thumbnailText = savePanel.transform.Find("Background/Left/TextBox/Text").GetComponent<Text>();
            var headerPanel = savePanel.transform.Find("Background/Right/Bottom").gameObject;
            saveButton = headerPanel.transform.Find("SaveButton").GetComponent<Button>();
            loadButton = headerPanel.transform.Find("LoadButton").GetComponent<Button>();
            var pagerPanel = headerPanel.transform.Find("Pager").gameObject;
            var leftButtonPanel = pagerPanel.transform.Find("LeftButton").gameObject;
            leftButton = leftButtonPanel.GetComponent<Button>();
            // leftButtonText = leftButtonPanel.GetComponent<Text>();
            var rightButtonPanel = pagerPanel.transform.Find("RightButton").gameObject;
            rightButton = rightButtonPanel.GetComponent<Button>();
            // rightButtonText = rightButtonPanel.GetComponent<Text>();
            pageText = pagerPanel.transform.Find("PageText").GetComponent<Text>();

            backgroundButton.onClick.AddListener(() => { selectedSaveId = -1; });
            if (canSave)
            {
                saveButton.onClick.AddListener(() => ShowSave());
            }
            else
            {
                // Cannot SetActive(false), otherwise layout will break
                saveButton.GetComponent<CanvasGroup>().alpha = 0.0f;
            }

            loadButton.onClick.AddListener(() => ShowLoad());
            leftButton.onClick.AddListener(() => PageLeft());
            rightButton.onClick.AddListener(() => PageRight());

            var saveEntryGrid = savePanel.transform.Find("Background/Right/Top").gameObject;
            for (int rowIdx = 0; rowIdx < maxRow; ++rowIdx)
            {
                var saveEntryRow = Instantiate(SaveEntryRowPrefab);
                saveEntryRow.transform.SetParent(saveEntryGrid.transform);
                saveEntryRow.transform.localScale = Vector3.one;
                for (int colIdx = 0; colIdx < maxCol; ++colIdx)
                {
                    var saveEntry = Instantiate(SaveEntryPrefab);
                    saveEntry.transform.SetParent(saveEntryRow.transform);
                    saveEntry.transform.localScale = Vector3.one;
                    saveEntries.Add(saveEntry);
                }
            }

            screenCapturer = gameObject.GetComponent<ScreenCapturer>();

            alertController = GameObject.FindWithTag("Alert").GetComponent<AlertController>();

            gameState.DialogueChanged += OnDialogueChanged;
        }

        private void Start()
        {
            ShowPage();
        }

        private void OnDestroy()
        {
            gameState.DialogueChanged -= OnDialogueChanged;
        }

        private void OnDialogueChanged(DialogueChangedData dialogueChangedData)
        {
            currentNodeName = dialogueChangedData.nodeName;
            currentDialogueText = dialogueChangedData.text;
        }

        private void Show()
        {
            if (!savePanel.activeSelf)
            {
                screenTexture = screenCapturer.GetTexture();
                screenSprite = Utils.Texture2DToSprite(screenTexture);
            }

            savePanel.SetActive(true);
            selectedSaveId = -1;
            ShowPage();
        }

        public void ShowSave()
        {
            saveViewMode = SaveViewMode.Save;
            if (!savePanel.activeSelf)
            {
                // Locate to the first unused slot
                saveViewBookmarkType = BookmarkType.NormalSave;
                int minUnusedSaveId = checkpointManager.QueryMinUnusedSaveId((int)BookmarkType.NormalSave, int.MaxValue);
                page = SaveIdToPage(minUnusedSaveId);
            }
            // Cannot see auto save and quick save in save mode
            if (saveViewBookmarkType != BookmarkType.NormalSave)
            {
                saveViewBookmarkType = BookmarkType.NormalSave;
                page = 1;
            }
            Show();
        }

        public void ShowLoad()
        {
            if (!savePanel.activeSelf)
            {
                // Locate to the latest slot
                saveViewBookmarkType = BookmarkType.NormalSave;
                int latestSaveId = checkpointManager.QuerySaveIdByTime((int)BookmarkType.NormalSave, int.MaxValue, SaveIdQueryType.Latest);
                page = SaveIdToPage(latestSaveId);
            }
            saveViewMode = SaveViewMode.Load;
            Show();
        }

        public void Hide()
        {
            if (savePanel.activeSelf)
            {
                Destroy(screenTexture);
                Destroy(screenSprite);
                screenTexture = null;
                screenSprite = null;
            }

            savePanel.SetActive(false);
        }

        private void PageLeft()
        {
            if (page == 1)
            {
                // Cannot see auto save and quick save in save mode
                if (saveViewMode == SaveViewMode.Load)
                {
                    if (saveViewBookmarkType == BookmarkType.QuickSave)
                    {
                        saveViewBookmarkType = BookmarkType.AutoSave;
                        page = 1;
                    }
                    else if (saveViewBookmarkType == BookmarkType.NormalSave)
                    {
                        saveViewBookmarkType = BookmarkType.QuickSave;
                        page = 1;
                    }
                }
            }
            else
            {
                --page;
            }
            ShowPage();
        }

        private void PageRight()
        {
            if (page == maxPage)
            {
                if (saveViewBookmarkType == BookmarkType.AutoSave)
                {
                    saveViewBookmarkType = BookmarkType.QuickSave;
                    page = 1;
                }
                else if (saveViewBookmarkType == BookmarkType.QuickSave)
                {
                    saveViewBookmarkType = BookmarkType.NormalSave;
                    page = 1;
                }
            }
            else
            {
                ++page;
            }
            ShowPage();
        }

        private void _saveBookmark(int saveId)
        {
            var bookmark = gameState.GetBookmark();
            bookmark.ScreenShot = screenSprite.texture;
            DeleteCachedThumbnailSprite(saveId);
            BookmarkSave.Invoke(new BookmarkSaveEventData(saveId, bookmark));
        }

        private void SaveBookmark(int saveId)
        {
            alertController.Alert(
                null,
                I18n.__("bookmark.overwrite.confirm", SaveIdToDisplayId(saveId)),
                () => _saveBookmark(saveId)
            );
        }

        private void _loadBookmark(int saveId)
        {
            var bookmark = checkpointManager.LoadBookmark(saveId);
            BookmarkLoad.Invoke(new BookmarkLoadEventData(bookmark));
        }

        private void LoadBookmark(int saveId)
        {
            alertController.Alert(
                null,
                I18n.__("bookmark.load.confirm", SaveIdToDisplayId(saveId)),
                () => _loadBookmark(saveId)
            );
        }

        private void _deleteBookmark(int saveId)
        {
            DeleteCachedThumbnailSprite(saveId);
            BookmarkDelete.Invoke(new BookmarkDeleteEventData(saveId));
        }

        private void DeleteBookmark(int saveId)
        {
            alertController.Alert(
                null,
                I18n.__("bookmark.delete.confirm", SaveIdToDisplayId(saveId)),
                () => _deleteBookmark(saveId)
            );
        }

        private void _autoSaveBookmark(int beginSaveId, string tagText)
        {
            var bookmark = gameState.GetBookmark();
            var texture = screenCapturer.GetTexture();
            bookmark.ScreenShot = texture;
            bookmark.Description = string.Format("【{0}】{1}", tagText, bookmark.Description);
            int saveId = checkpointManager.QueryMinUnusedSaveId(beginSaveId, beginSaveId + maxSaveEntry);
            if (saveId >= beginSaveId + maxSaveEntry)
            {
                saveId = checkpointManager.QuerySaveIdByTime(beginSaveId, beginSaveId + maxSaveEntry, SaveIdQueryType.Earliest);
            }
            BookmarkSave.Invoke(new BookmarkSaveEventData(saveId, bookmark));
            Destroy(texture);
        }

        public void AutoSaveBookmark()
        {
            _autoSaveBookmark((int)BookmarkType.AutoSave, I18n.__("bookmark.autosave.page"));
        }

        private void _quickSaveBookmark()
        {
            _autoSaveBookmark((int)BookmarkType.QuickSave, I18n.__("bookmark.quicksave.page"));
        }

        public void QuickSaveBookmark()
        {
            alertController.Alert(null, I18n.__("bookmark.quicksave.confirm"), () => _quickSaveBookmark());
        }

        private void _quickLoadBookmark()
        {
            int saveId = checkpointManager.QuerySaveIdByTime((int)BookmarkType.QuickSave, (int)BookmarkType.NormalSave, SaveIdQueryType.Latest);
            var bookmark = checkpointManager.LoadBookmark(saveId);
            BookmarkLoad.Invoke(new BookmarkLoadEventData(bookmark));
        }

        public void QuickLoadBookmark()
        {
            if (checkpointManager.SaveSlotsMetadata.Values.Where(
                m => m.SaveId >= (int)BookmarkType.QuickSave && m.SaveId < (int)BookmarkType.QuickSave + maxSaveEntry
            ).Any())
            {
                alertController.Alert(null, I18n.__("bookmark.quickload.confirm"), () => _quickLoadBookmark());
            }
            else
            {
                alertController.Alert(null, I18n.__("bookmark.quickload.nosave"));
            }
        }

        private void OnThumbnailButtonClicked(int saveId)
        {
            if (Input.touchCount == 0) // Mouse
            {
                if (saveViewMode == SaveViewMode.Save)
                {
                    if (checkpointManager.SaveSlotsMetadata.ContainsKey(saveId))
                    {
                        SaveBookmark(saveId);
                    }
                    else // Bookmark with this saveId does not exist
                    {
                        // No alert when saving to an empty slot
                        _saveBookmark(saveId);
                    }
                }
                else // saveViewMode == SaveViewMode.Load
                {
                    if (checkpointManager.SaveSlotsMetadata.ContainsKey(saveId))
                    {
                        LoadBookmark(saveId);
                    }
                }
            }
            else // Touch
            {
                if (saveViewMode == SaveViewMode.Save)
                {
                    if (saveId == selectedSaveId)
                    {
                        SaveBookmark(saveId);
                    }
                    else // Another bookmark selected
                    {
                        if (checkpointManager.SaveSlotsMetadata.ContainsKey(saveId))
                        {
                            selectedSaveId = saveId;
                        }
                        else // Bookmark with this saveId does not exist
                        {
                            selectedSaveId = -1;
                            // No alert when saving to an empty slot
                            _saveBookmark(saveId);
                        }
                    }
                }
                else // saveViewMode == SaveViewMode.Load
                {
                    if (saveId == selectedSaveId)
                    {
                        LoadBookmark(saveId);
                    }
                    else // Another bookmark selected
                    {
                        if (checkpointManager.SaveSlotsMetadata.ContainsKey(saveId))
                        {
                            selectedSaveId = saveId;
                        }
                        else // Bookmark with this saveId does not exist
                        {
                            selectedSaveId = -1;
                        }
                    }
                }
            }
        }

        private void OnThumbnailButtonEnter(int saveId)
        {
            if (Input.touchCount == 0) // Mouse
            {
                if (checkpointManager.SaveSlotsMetadata.ContainsKey(saveId))
                {
                    selectedSaveId = saveId;
                }
            }
        }

        private void OnThumbnailButtonExit(int saveId)
        {
            if (Input.touchCount == 0) // Mouse
            {
                selectedSaveId = -1;
            }
        }

        private void ShowPreview(Sprite newThumbnailSprite, string newText)
        {
            if (newThumbnailSprite == null)
            {
                thumbnailImage.sprite = defaultThumbnailSprite;
            }
            else
            {
                thumbnailImage.sprite = newThumbnailSprite;
            }

            thumbnailText.text = newText;
        }

        private void ShowPreviewScreen()
        {
            ShowPreview(screenSprite, I18n.__(
                "bookmark.summary",
                DateTime.Now.ToString(dateTimeFormat),
                currentNodeName,
                currentDialogueText
            ));
        }

        private void ShowPreviewBookmark(int saveId)
        {
            Bookmark bookmark = checkpointManager[saveId];
            ShowPreview(GetThumbnailSprite(saveId), I18n.__(
                "bookmark.summary",
                checkpointManager.SaveSlotsMetadata[saveId].ModifiedTime.ToString(dateTimeFormat),
                bookmark.NodeHistory.Last(),
                bookmark.Description
            ));
        }

        public void ShowPage()
        {
            if (saveViewBookmarkType == BookmarkType.NormalSave)
            {
                int maxSaveId = checkpointManager.QueryMaxSaveId((int)BookmarkType.NormalSave);
                if (checkpointManager.SaveSlotsMetadata.ContainsKey(maxSaveId))
                {
                    maxPage = SaveIdToPage(maxSaveId);
                    if (saveViewMode == SaveViewMode.Save)
                    {
                        // New page to save
                        ++maxPage;
                    }
                }
                else
                {
                    maxPage = 1;
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

            if (saveViewBookmarkType == BookmarkType.AutoSave)
            {
                pageText.text = I18n.__("bookmark.autosave.page");
            }
            else if (saveViewBookmarkType == BookmarkType.QuickSave)
            {
                pageText.text = I18n.__("bookmark.quicksave.page");
            }
            else // saveViewBookmarkType == BookmarkType.NormalSave
            {
                pageText.text = string.Format("{0} / {1}", page, maxPage);
            }

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

            for (int i = 0; i < maxSaveEntry; ++i)
            {
                int saveId = (page - 1) * maxSaveEntry + i + (int)saveViewBookmarkType;
                string newIdText = SaveIdToDisplayId(saveId).ToString();

                // Load properties from bookmark
                string newHeaderText;
                string newFooterText;
                Sprite newThumbnailSprite;
                UnityAction onEditButtonClicked;
                UnityAction onDeleteButtonClicked;
                if (checkpointManager.SaveSlotsMetadata.ContainsKey(saveId))
                {
                    Bookmark bookmark = checkpointManager[saveId];
                    newHeaderText = bookmark.NodeHistory.Last();
                    newFooterText = bookmark.CreationTime.ToString(dateTimeFormat);
                    newThumbnailSprite = GetThumbnailSprite(saveId);
                    onEditButtonClicked = null;
                    onDeleteButtonClicked = () => DeleteBookmark(saveId);
                }
                else
                {
                    newHeaderText = "";
                    newFooterText = "";
                    newThumbnailSprite = null;
                    onEditButtonClicked = null;
                    onDeleteButtonClicked = null;
                }

                UnityAction onThumbnailButtonClicked = () => OnThumbnailButtonClicked(saveId);
                UnityAction onThumbnailButtonEnter = () => OnThumbnailButtonEnter(saveId);
                UnityAction onThumbnailButtonExit = () => OnThumbnailButtonExit(saveId);

                // Update UI of saveEntry
                var saveEntry = saveEntries[i];
                var saveEntryController = saveEntry.GetComponent<SaveEntryController>();
                saveEntryController.Init(newIdText, newHeaderText, newFooterText, newThumbnailSprite,
                    onEditButtonClicked, onDeleteButtonClicked,
                    onThumbnailButtonClicked, onThumbnailButtonEnter, onThumbnailButtonExit);
            }
        }

        private Sprite GetThumbnailSprite(int saveId)
        {
            Assert.IsTrue(checkpointManager.SaveSlotsMetadata.ContainsKey(saveId),
                "Nova: GetThumbnailSprite must use a saveId with existing bookmark");
            if (!_cachedThumbnailSprite.ContainsKey(saveId))
            {
                Bookmark bookmark = checkpointManager[saveId];
                _cachedThumbnailSprite[saveId] = Utils.Texture2DToSprite(bookmark.ScreenShot);
            }

            return _cachedThumbnailSprite[saveId];
        }

        private void DeleteCachedThumbnailSprite(int saveId)
        {
            if (_cachedThumbnailSprite.ContainsKey(saveId))
            {
                Destroy(_cachedThumbnailSprite[saveId]);
                _cachedThumbnailSprite.Remove(saveId);
            }
        }

        private int SaveIdToPage(int saveId)
        {
            return (saveId - (int)BookmarkMetadata.SaveIdToBookmarkType(saveId) + maxSaveEntry) / maxSaveEntry;
        }

        private int SaveIdToDisplayId(int saveId)
        {
            return saveId - (int)BookmarkMetadata.SaveIdToBookmarkType(saveId) + 1;
        }
    }
}