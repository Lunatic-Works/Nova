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

    public class SaveViewController : ViewControllerBase
    {
        [SerializeField] private GameObject saveEntryPrefab;
        [SerializeField] private GameObject saveEntryRowPrefab;
        [SerializeField] private int maxRow;
        [SerializeField] private int maxCol;

        [SerializeField] private AudioClip saveActionSound;
        [SerializeField] private AudioClip loadActionSound;
        [SerializeField] private AudioClip deleteActionSound;

        private GameState gameState;
        private CheckpointManager checkpointManager;

        private Button backgroundButton;
        private SaveEntryController previewEntry;
        private TextProxy thumbnailTextProxy;

        private Button saveButton;
        private Button loadButton;
        private Text saveText;
        private Text loadText;
        private CanvasGroup saveButtonCanvasGroup;

        private Button leftButton;
        private Button rightButton;
        private Text leftButtonText;
        private Text rightButtonText;
        private Text pageText;

        private Color defaultTextColor;
        private Color saveTextColor;
        private Color loadTextColor;
        private Color disabledTextColor;

        private Sprite corruptedThumbnailSprite;

        private readonly List<SaveEntryController> saveEntryControllers = new List<SaveEntryController>();
        private readonly Dictionary<int, Sprite> cachedThumbnailSprites = new Dictionary<int, Sprite>();

        private int maxSaveEntry;
        private int page = 1;

        // maxPage is updated when ShowPage is called
        private int maxPage = 1;

        // selectedSaveID == -1 means no bookmark is selected
        private int _selectedSaveID = -1;

        private int selectedSaveID
        {
            get => _selectedSaveID;

            set
            {
                this.RuntimeAssert(checkpointManager.saveSlotsMetadata.ContainsKey(value) || value == -1,
                    "selectedSaveID must be a saveID with existing bookmark, or -1.");

                if (_selectedSaveID >= 0)
                {
                    SaveIDToSaveEntryController(_selectedSaveID).HideDeleteButton();
                }

                _selectedSaveID = value;

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
        private bool fromTitle;

        // screenTexture and screenSprite are created when Show is called and savePanel is not active
        // They are destroyed when Hide is called and savePanel is active
        private Texture2D screenTexture;
        private Sprite screenSprite;

        private const string DateTimeFormat = "yyyy/MM/dd  HH:mm";

        private FlowChartNode currentNode;
        private DialogueDisplayData currentDialogue;

        protected override void Awake()
        {
            base.Awake();

            maxSaveEntry = maxRow * maxCol;

            gameState = Utils.FindNovaGameController().GameState;
            checkpointManager = Utils.FindNovaGameController().CheckpointManager;

            backgroundButton = myPanel.transform.Find("Background").GetComponent<Button>();
            thumbnailTextProxy = myPanel.transform.Find("Background/Left/TextBox/Text").GetComponent<TextProxy>();

            var headerPanel = myPanel.transform.Find("Background/Right/Bottom");
            var saveButtonPanel = headerPanel.Find("SaveButton");
            var loadButtonPanel = headerPanel.Find("LoadButton");
            saveButton = saveButtonPanel.GetComponent<Button>();
            loadButton = loadButtonPanel.GetComponent<Button>();
            saveText = saveButtonPanel.GetComponentInChildren<Text>();
            loadText = loadButtonPanel.GetComponentInChildren<Text>();
            saveButtonCanvasGroup = saveButton.GetComponent<CanvasGroup>();

            var pagerPanel = headerPanel.Find("Pager");
            var leftButtonPanel = pagerPanel.Find("LeftButton");
            var rightButtonPanel = pagerPanel.Find("RightButton");
            leftButton = leftButtonPanel.GetComponent<Button>();
            rightButton = rightButtonPanel.GetComponent<Button>();
            leftButtonText = leftButtonPanel.GetComponentInChildren<Text>();
            rightButtonText = rightButtonPanel.GetComponentInChildren<Text>();
            pageText = pagerPanel.Find("PageText").GetComponent<Text>();

            ColorUtility.TryParseHtmlString("#000000FF", out defaultTextColor);
            ColorUtility.TryParseHtmlString("#33CC33FF", out saveTextColor);
            ColorUtility.TryParseHtmlString("#CC3333FF", out loadTextColor);
            ColorUtility.TryParseHtmlString("#808080FF", out disabledTextColor);

            corruptedThumbnailSprite = Utils.Texture2DToSprite(Utils.ClearTexture);

            backgroundButton.onClick.AddListener(Unselect);
            saveButton.onClick.AddListener(ShowSave);
            loadButton.onClick.AddListener(ShowLoad);
            leftButton.onClick.AddListener(PageLeft);
            rightButton.onClick.AddListener(PageRight);

            var saveEntryGrid = myPanel.transform.Find("Background/Right/Top");
            for (int rowIdx = 0; rowIdx < maxRow; ++rowIdx)
            {
                var saveEntryRow = Instantiate(saveEntryRowPrefab, saveEntryGrid.transform);
                for (int colIdx = 0; colIdx < maxCol; ++colIdx)
                {
                    var saveEntry = Instantiate(saveEntryPrefab, saveEntryRow.transform);
                    saveEntryControllers.Add(saveEntry.GetComponent<SaveEntryController>());
                }
            }

            gameState.nodeChanged.AddListener(OnNodeChanged);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);
        }

        protected override void Start()
        {
            base.Start();

            checkpointManager.Init();
            previewEntry = myPanel.transform.Find("Background/Left/SaveEntry").GetComponent<SaveEntryController>();
            previewEntry.InitAsPreview(null, Hide);
            ShowPage();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            backgroundButton.onClick.RemoveListener(Unselect);
            saveButton.onClick.RemoveListener(ShowSave);
            loadButton.onClick.RemoveListener(ShowLoad);
            leftButton.onClick.RemoveListener(PageLeft);
            rightButton.onClick.RemoveListener(PageRight);

            gameState.nodeChanged.RemoveListener(OnNodeChanged);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
        }

        private void OnNodeChanged(NodeChangedData nodeChangedData)
        {
            currentNode = gameState.GetNode(nodeChangedData.nodeHistoryEntry.Key);
        }

        private void OnDialogueChanged(DialogueChangedData dialogueChangedData)
        {
            currentDialogue = dialogueChangedData.displayData;
        }

        #region Show and hide

        private void Show(SaveViewMode newSaveViewMode, bool newFromTitle)
        {
            saveViewMode = newSaveViewMode;
            fromTitle = newFromTitle;

            // Initialize page
            if (myPanel.activeSelf)
            {
                // Cannot see auto save and quick save in save mode
                if (saveViewMode == SaveViewMode.Save && saveViewBookmarkType != BookmarkType.NormalSave)
                {
                    saveViewBookmarkType = BookmarkType.NormalSave;
                    page = 1;
                }
            }
            else
            {
                saveViewBookmarkType = BookmarkType.NormalSave;
                int saveID;
                if (saveViewMode == SaveViewMode.Save)
                {
                    // Locate to the first unused slot
                    saveID = checkpointManager.QueryMinUnusedSaveID((int)BookmarkType.NormalSave, int.MaxValue);
                }
                else // saveViewMode == SaveViewMode.Load
                {
                    // Locate to the latest slot
                    saveID = checkpointManager.QuerySaveIDByTime((int)BookmarkType.NormalSave, int.MaxValue,
                        SaveIDQueryType.Latest);
                }

                page = SaveIDToPage(saveID);
            }

            // Hide save button and current node if from title
            if (fromTitle)
            {
                // Cannot SetActive(false), otherwise layout will break
                saveButtonCanvasGroup.alpha = 0.0f;
                currentNode = null;
                currentDialogue = null;
            }
            else
            {
                saveButtonCanvasGroup.alpha = 1.0f;
            }

            if (!myPanel.activeSelf)
            {
                if (screenTexture != null)
                {
                    Destroy(screenTexture);
                }

                screenTexture = ScreenCapturer.GetBookmarkThumbnailTexture();
                screenSprite = Utils.Texture2DToSprite(screenTexture);
            }

            selectedSaveID = -1;

            ShowPage();

            base.Show();
        }

        public void ShowSave()
        {
            // Cannot enter save mode if from title
            if (myPanel.activeSelf && fromTitle)
            {
                return;
            }

            Show(SaveViewMode.Save, false);
        }

        public void ShowLoad()
        {
            Show(SaveViewMode.Load, false);
        }

        public void ShowLoadFromTitle()
        {
            Show(SaveViewMode.Load, true);
        }

        protected override void OnHideComplete()
        {
            if (myPanel.activeSelf)
            {
                if (screenTexture != null)
                {
                    Destroy(screenTexture);
                    screenTexture = null;
                }

                if (screenSprite != null)
                {
                    Destroy(screenSprite);
                    screenSprite = null;
                }
            }

            base.OnHideComplete();
        }

        #endregion

        #region Bookmark operations

        private void _saveBookmark(int saveID)
        {
            var bookmark = gameState.GetBookmark();
            bookmark.screenshot = screenSprite.texture;
            DeleteCachedThumbnailSprite(saveID);
            checkpointManager.SaveBookmark(saveID, bookmark);

            ShowPage();
            ShowPreviewBookmark(saveID);
            viewManager.TryPlaySound(saveActionSound);
        }

        private void SaveBookmark(int saveID)
        {
            Alert.Show(
                null,
                I18n.__("bookmark.overwrite.confirm", SaveIDToDisplayID(saveID)),
                () => _saveBookmark(saveID),
                null,
                "BookmarkOverwrite"
            );
        }

        private void _loadBookmark(int saveID)
        {
            var bookmark = checkpointManager.LoadBookmark(saveID);
            DeleteCachedThumbnailSprite(saveID);
            gameState.LoadBookmark(bookmark);

            if (viewManager.titlePanel.activeSelf)
            {
                viewManager.titlePanel.SetActive(false);
                viewManager.dialoguePanel.SetActive(true);
            }

            Hide();
            viewManager.TryPlaySound(loadActionSound);
            Alert.Show(I18n.__("bookmark.load.complete"));
        }

        private void LoadBookmark(int saveID)
        {
            Alert.Show(
                null,
                I18n.__("bookmark.load.confirm", SaveIDToDisplayID(saveID)),
                () => _loadBookmark(saveID),
                null,
                "BookmarkLoad"
            );
        }

        private void _deleteBookmark(int saveID)
        {
            DeleteCachedThumbnailSprite(saveID);
            checkpointManager.DeleteBookmark(saveID);

            ShowPage();
            selectedSaveID = -1;
            viewManager.TryPlaySound(deleteActionSound);
        }

        private void DeleteBookmark(int saveID)
        {
            Alert.Show(
                null,
                I18n.__("bookmark.delete.confirm", SaveIDToDisplayID(saveID)),
                () => _deleteBookmark(saveID),
                null,
                "BookmarkDelete"
            );
        }

        private void _autoSaveBookmark(int beginSaveID, string tagText)
        {
            var bookmark = gameState.GetBookmark();
            var texture = ScreenCapturer.GetBookmarkThumbnailTexture();
            bookmark.screenshot = texture;

            int saveID = checkpointManager.QueryMinUnusedSaveID(beginSaveID, beginSaveID + maxSaveEntry);
            if (saveID >= beginSaveID + maxSaveEntry)
            {
                saveID = checkpointManager.QuerySaveIDByTime(beginSaveID, beginSaveID + maxSaveEntry,
                    SaveIDQueryType.Earliest);
            }

            checkpointManager.SaveBookmark(saveID, bookmark);
            Destroy(texture);
        }

        public void AutoSaveBookmark()
        {
            _autoSaveBookmark((int)BookmarkType.AutoSave, I18n.__("bookmark.autosave.page"));
        }

        private void _quickSaveBookmark()
        {
            _autoSaveBookmark((int)BookmarkType.QuickSave, I18n.__("bookmark.quicksave.page"));
            viewManager.TryPlaySound(saveActionSound);
            Alert.Show(I18n.__("bookmark.quicksave.complete"));
        }

        public void QuickSaveBookmark()
        {
            Alert.Show(
                null,
                I18n.__("bookmark.quicksave.confirm"),
                _quickSaveBookmark,
                null,
                "BookmarkQuickSave"
            );
        }

        private void _quickLoadBookmark()
        {
            int saveID = checkpointManager.QuerySaveIDByTime((int)BookmarkType.QuickSave,
                (int)BookmarkType.NormalSave, SaveIDQueryType.Latest);
            var bookmark = checkpointManager.LoadBookmark(saveID);
            DeleteCachedThumbnailSprite(saveID);
            gameState.LoadBookmark(bookmark);

            viewManager.TryPlaySound(loadActionSound);
            Alert.Show(I18n.__("bookmark.load.complete"));
        }

        public void QuickLoadBookmark()
        {
            if (checkpointManager.saveSlotsMetadata.Values.Any(m =>
                    m.saveID >= (int)BookmarkType.QuickSave && m.saveID < (int)BookmarkType.QuickSave + maxSaveEntry))
            {
                Alert.Show(
                    null,
                    I18n.__("bookmark.quickload.confirm"),
                    _quickLoadBookmark,
                    null,
                    "BookmarkQuickLoad"
                );
            }
            else
            {
                Alert.Show(null, I18n.__("bookmark.quickload.nosave"));
            }
        }

        #endregion

        #region Preview

        private void OnThumbnailButtonClicked(int saveID)
        {
            if (Input.touchCount == 0) // Mouse
            {
                if (saveViewMode == SaveViewMode.Save)
                {
                    if (checkpointManager.saveSlotsMetadata.ContainsKey(saveID))
                    {
                        SaveBookmark(saveID);
                    }
                    else // Bookmark with this saveID does not exist
                    {
                        // No alert when saving to an empty slot
                        _saveBookmark(saveID);
                    }
                }
                else // saveViewMode == SaveViewMode.Load
                {
                    if (checkpointManager.saveSlotsMetadata.ContainsKey(saveID))
                    {
                        LoadBookmark(saveID);
                    }
                }
            }
            else // Touch
            {
                if (saveViewMode == SaveViewMode.Save)
                {
                    if (saveID == selectedSaveID)
                    {
                        SaveBookmark(saveID);
                    }
                    else // Another bookmark selected
                    {
                        if (checkpointManager.saveSlotsMetadata.ContainsKey(saveID))
                        {
                            selectedSaveID = saveID;
                        }
                        else // Bookmark with this saveID does not exist
                        {
                            selectedSaveID = -1;
                            // No alert when saving to an empty slot
                            _saveBookmark(saveID);
                        }
                    }
                }
                else // saveViewMode == SaveViewMode.Load
                {
                    if (saveID == selectedSaveID)
                    {
                        LoadBookmark(saveID);
                    }
                    else // Another bookmark selected
                    {
                        if (checkpointManager.saveSlotsMetadata.ContainsKey(saveID))
                        {
                            selectedSaveID = saveID;
                        }
                        else // Bookmark with this saveID does not exist
                        {
                            selectedSaveID = -1;
                        }
                    }
                }
            }
        }

        private void OnThumbnailButtonEnter(int saveID)
        {
            if (viewManager.currentView != CurrentViewType.UI)
            {
                return;
            }

            if (Input.touchCount == 0) // Mouse
            {
                if (checkpointManager.saveSlotsMetadata.ContainsKey(saveID))
                {
                    selectedSaveID = saveID;
                }
            }
        }

        private void OnThumbnailButtonExit(int saveID)
        {
            if (viewManager.currentView != CurrentViewType.UI)
            {
                return;
            }

            if (Input.touchCount == 0) // Mouse
            {
                selectedSaveID = -1;
            }
        }

        private void Unselect()
        {
            selectedSaveID = -1;
        }

        private void ShowPreview(Sprite newThumbnailSprite, string newText)
        {
            previewEntry.InitAsPreview(newThumbnailSprite, Hide);
            thumbnailTextProxy.text = newText;
        }

        private void ShowPreviewScreen()
        {
            ShowPreview(screenSprite, I18n.__(
                "bookmark.summary",
                fromTitle ? "" : DateTime.Now.ToString(DateTimeFormat),
                currentNode != null ? I18n.__(currentNode.displayNames) : "",
                currentDialogue != null ? currentDialogue.FormatNameDialogue() : ""
            ));
        }

        private void ShowPreviewBookmark(int saveID)
        {
            try
            {
                Bookmark bookmark = checkpointManager[saveID];
                var nodeName = checkpointManager.GetLastNodeName(bookmark.nodeHistoryHash);
                var node = gameState.GetNode(nodeName);
                var displayName = I18n.__(node.displayNames);
                var description = bookmark.description;
                if (string.IsNullOrEmpty(description))
                {
                    description = node.GetDialogueEntryAt(bookmark.dialogueIndex).GetDisplayData().FormatNameDialogue();
                }

                ShowPreview(GetThumbnailSprite(saveID), I18n.__(
                    "bookmark.summary",
                    checkpointManager.saveSlotsMetadata[saveID].modifiedTime.ToString(DateTimeFormat),
                    displayName,
                    description
                ));
            }
            catch (Exception e)
            {
                // TODO: do not load a bookmark multiple times when it is corrupted
                Debug.LogWarning(e);
                ShowPreview(corruptedThumbnailSprite, I18n.__("bookmark.corrupted.title"));
            }
        }

        #endregion

        #region Page

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

        public void ShowPage()
        {
            saveButton.interactable = (saveViewMode != SaveViewMode.Save);
            loadButton.interactable = (saveViewMode != SaveViewMode.Load);
            saveText.color = (saveButton.interactable ? disabledTextColor : saveTextColor);
            loadText.color = (loadButton.interactable ? disabledTextColor : loadTextColor);

            if (saveViewBookmarkType == BookmarkType.NormalSave)
            {
                int maxSaveID = checkpointManager.QueryMaxSaveID((int)BookmarkType.NormalSave);
                if (checkpointManager.saveSlotsMetadata.ContainsKey(maxSaveID))
                {
                    maxPage = SaveIDToPage(maxSaveID);
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
                pageText.text = $"{page} / {maxPage}";
            }

            leftButton.interactable = (page > 1 ||
                                       (saveViewMode == SaveViewMode.Load &&
                                        saveViewBookmarkType != BookmarkType.AutoSave));
            rightButton.interactable = (page < maxPage || saveViewBookmarkType != BookmarkType.NormalSave);
            leftButtonText.color = (leftButton.interactable ? defaultTextColor : disabledTextColor);
            rightButtonText.color = (rightButton.interactable ? defaultTextColor : disabledTextColor);

            int latestSaveID =
                checkpointManager.QuerySaveIDByTime((int)BookmarkType.NormalSave, int.MaxValue,
                    SaveIDQueryType.Latest);

            for (int i = 0; i < maxSaveEntry; ++i)
            {
                int saveID = (page - 1) * maxSaveEntry + i + (int)saveViewBookmarkType;
                string newIDText = SaveIDToDisplayID(saveID).ToString();

                // Load properties from bookmark
                string newHeaderText;
                string newFooterText;
                Sprite newThumbnailSprite;
                UnityAction onEditButtonClicked;
                UnityAction onDeleteButtonClicked;
                UnityAction onThumbnailButtonClicked = null;

                if (checkpointManager.saveSlotsMetadata.ContainsKey(saveID))
                {
                    try
                    {
                        Bookmark bookmark = checkpointManager[saveID];
                        var nodeName = checkpointManager.GetLastNodeName(bookmark.nodeHistoryHash);
                        newHeaderText = I18n.__(gameState.GetNode(nodeName).displayNames);
                        newFooterText = bookmark.creationTime.ToString(DateTimeFormat);
                        newThumbnailSprite = GetThumbnailSprite(saveID);
                        onEditButtonClicked = null;
                        onDeleteButtonClicked = () => DeleteBookmark(saveID);

                        onThumbnailButtonClicked = () => OnThumbnailButtonClicked(saveID);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                        newHeaderText = "";
                        newFooterText = I18n.__("bookmark.corrupted.title");
                        newThumbnailSprite = corruptedThumbnailSprite;
                        onEditButtonClicked = null;
                        onDeleteButtonClicked = () => DeleteBookmark(saveID);
                        onThumbnailButtonClicked = null;
                    }
                }
                else
                {
                    newHeaderText = "";
                    newFooterText = "";
                    newThumbnailSprite = null;
                    onEditButtonClicked = null;
                    onDeleteButtonClicked = null;

                    if (saveViewMode == SaveViewMode.Save)
                    {
                        onThumbnailButtonClicked = () => OnThumbnailButtonClicked(saveID);
                    }
                }

                UnityAction onThumbnailButtonEnter = () => OnThumbnailButtonEnter(saveID);
                UnityAction onThumbnailButtonExit = () => OnThumbnailButtonExit(saveID);

                // Update UI of saveEntry
                var saveEntryController = saveEntryControllers[i];
                saveEntryController.mode = saveViewMode;
                saveEntryController.Init(newIDText, newHeaderText, newFooterText, saveID == latestSaveID,
                    newThumbnailSprite, onEditButtonClicked, onDeleteButtonClicked, onThumbnailButtonClicked,
                    onThumbnailButtonEnter, onThumbnailButtonExit);
            }

            previewEntry.mode = saveViewMode;
        }

        #endregion

        private Sprite GetThumbnailSprite(int saveID)
        {
            this.RuntimeAssert(checkpointManager.saveSlotsMetadata.ContainsKey(saveID),
                "GetThumbnailSprite must use a saveID with existing bookmark.");
            if (!cachedThumbnailSprites.ContainsKey(saveID))
            {
                Bookmark bookmark = checkpointManager[saveID];
                cachedThumbnailSprites[saveID] = Utils.Texture2DToSprite(bookmark.screenshot);
            }

            return cachedThumbnailSprites[saveID];
        }

        private void DeleteCachedThumbnailSprite(int saveID)
        {
            if (cachedThumbnailSprites.ContainsKey(saveID))
            {
                Destroy(cachedThumbnailSprites[saveID]);
                cachedThumbnailSprites.Remove(saveID);
            }
        }

        private int SaveIDToPage(int saveID)
        {
            return (saveID - (int)BookmarkMetadata.SaveIDToBookmarkType(saveID) + maxSaveEntry) / maxSaveEntry;
        }

        private static int SaveIDToDisplayID(int saveID)
        {
            return saveID - (int)BookmarkMetadata.SaveIDToBookmarkType(saveID) + 1;
        }

        private SaveEntryController SaveIDToSaveEntryController(int saveID)
        {
            int i = (saveID - (int)BookmarkMetadata.SaveIDToBookmarkType(saveID)) % maxSaveEntry;
            if (i >= 0)
            {
                return saveEntryControllers[i];
            }
            else
            {
                return null;
            }
        }
    }
}