// TODO
// Page 0 for auto save, last page for new page
// Auto hide edit and delete buttons
// UI to edit bookmark's description
// Capture thumbnail
//
// Function to save and load bookmark
// Function to get bookmark's date and time, chapter name, description, thumbnail
// Function to update bookmark's description

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
        public int maxRow;
        public int maxCol;
        public Sprite noThumbnailSprite;
        public bool canSave;

        private int maxSaveEntry;
        private int page = 1;

        // When ShowPage is called, maxPage is updated
        // Use a data structure to maintain maximum for usedSaveSlots may improve performance
        private int maxPage = 1;

        private GameObject savePanel;
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

        private void Awake()
        {
            maxSaveEntry = maxRow * maxCol;

            savePanel = transform.Find("SavePanel").gameObject;
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
        }

        private void Start()
        {
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

            usedSaveSlots = gameState.checkpointManager.UsedSaveSlots;

            gameState.DialogueChanged.AddListener(OnDialogueChanged);

            ShowPage();
        }

        /// <summary>
        /// The name of the current flow chart node
        /// </summary>
        private string currentNodeName;

        /// <summary>
        /// The index of the current dialogue entry in the current node
        /// </summary>
        private int currentDialogueIndex;

        private void OnDialogueChanged(DialogueChangedEventData dialogueChangedEventData)
        {
            currentNodeName = dialogueChangedEventData.labelName;
            currentDialogueIndex = dialogueChangedEventData.dialogueIndex;
        }

        public void ShowSave()
        {
            savePanel.SetActive(true);
            saveViewMode = SaveViewMode.Save;
            ShowPage();
        }

        public void ShowLoad()
        {
            savePanel.SetActive(true);
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
            gameState.checkpointManager.SaveBookmark(saveId, bookmark);
            Hide();
        }

        private void LoadBookmark(int saveId)
        {
            var bookmark = gameState.checkpointManager.LoadBookmark(saveId);
            Debug.Log(bookmark.NodeHistory.Last());
            Debug.Log(bookmark.DialogueIndex);
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

            for (var i = 0; i < maxSaveEntry; ++i)
            {
                var saveEntry = saveEntries[i];
                var saveEntryController = saveEntry.GetComponent<SaveEntryController>();
                var saveId = (page - 1) * maxSaveEntry + i + 1;

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

                string newIdText;
                string newHeaderText;
                string newFooterText;
                UnityAction onThumbnailButtonClicked;
                UnityAction onEditButtonClicked;
                UnityAction onDeleteButtonClicked;
                Sprite newThumbnailSprite;
                if (usedSaveSlots.Contains(saveId))
                {
                    newIdText = "#" + saveId.ToString();
                    newHeaderText = "Chapter Name";
                    newFooterText = "1926/08/17 12:34";

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

                    newThumbnailSprite = null;
                }
                else // Bookmark with this saveId is not found
                {
                    newIdText = "#" + saveId.ToString();
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

                    newThumbnailSprite = noThumbnailSprite;
                }

                saveEntryController.Init(newIdText, newHeaderText, newFooterText,
                    onThumbnailButtonClicked, onEditButtonClicked, onDeleteButtonClicked,
                    newThumbnailSprite);
            }
        }
    }
}