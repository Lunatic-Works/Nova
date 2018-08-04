// TODO
// Change left text to edit bookmark's description
// Infinite pages, calculate maxPage from UsedSaveSlots
// Visual difference of save and load
// Call GameState's save and load functions
// Edit bookmark's description
// Show date and time, chapter name, description
// Show thumbnail

using System.Collections;
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
        public readonly int maxPage = 3;
        public readonly int maxSaveEntry = 9;
        public readonly int saveEntryPerRow = 3;
        public Sprite noThumbnailSprite;

        private GameObject savePanel;

        private GameObject saveButtonPanel;
        private GameObject loadButtonPanel;
        private Button saveButton;
        private Button loadButton;
        private Image saveButtonImage;
        private Image loadButtonImage;

        private Button leftButton;
        private Button rightButton;
        private Text pageText;
        private int page = 1;

        private readonly List<GameObject> saveEntries = new List<GameObject>();

        private SaveViewMode saveViewMode;

        private void Awake()
        {
            savePanel = transform.Find("SavePanel").gameObject;

            var bottom = savePanel.transform.Find("Background/Right/Bottom").gameObject;
            saveButtonPanel = bottom.transform.Find("SaveButton").gameObject;
            loadButtonPanel = bottom.transform.Find("LoadButton").gameObject;
            saveButton = saveButtonPanel.GetComponent<Button>();
            loadButton = loadButtonPanel.GetComponent<Button>();
            saveButtonImage = saveButtonPanel.GetComponent<Image>();
            loadButtonImage = loadButtonPanel.GetComponent<Image>();

            var pager = bottom.transform.Find("Pager").gameObject;
            leftButton = pager.transform.Find("LeftButton").gameObject.GetComponent<Button>();
            rightButton = pager.transform.Find("RightButton").gameObject.GetComponent<Button>();
            pageText = pager.transform.Find("PageText").gameObject.GetComponent<Text>();

            leftButton.onClick.AddListener(() => pageLeft());
            rightButton.onClick.AddListener(() => pageRight());
        }

        private void Start()
        {
            var saveEntryGrid = savePanel.transform.Find("Background/Right/Top").gameObject;
            for (var i = 0; i < maxSaveEntry; ++i)
            {
                var saveEntry = Instantiate(SaveEntryPrefab);
                saveEntries.Add(saveEntry);
                var saveEntryRow = saveEntryGrid.transform.Find(string.Format("Row{0}", i / saveEntryPerRow)).gameObject;
                saveEntry.transform.SetParent(saveEntryRow.transform);
            }

            gameState.DialogueChanged.AddListener(OnDialogueChanged);
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
            saveButtonImage.CrossFadeAlpha(1.0f, 0.1f, false);
            loadButtonImage.CrossFadeAlpha(0.1f, 0.1f, false);
            saveButton.interactable = false;
            loadButton.interactable = true;
            saveViewMode = SaveViewMode.Save;
            ShowPage();
        }

        public void ShowLoad()
        {
            savePanel.SetActive(true);
            saveButtonImage.CrossFadeAlpha(0.1f, 0.1f, false);
            loadButtonImage.CrossFadeAlpha(1.0f, 0.1f, false);
            saveButton.interactable = true;
            loadButton.interactable = false;
            saveViewMode = SaveViewMode.Load;
            ShowPage();
        }

        public void Hide()
        {
            savePanel.SetActive(false);
            saveButtonImage.CrossFadeAlpha(1.0f, 0.1f, false);
            loadButtonImage.CrossFadeAlpha(1.0f, 0.1f, false);
        }

        public void pageLeft()
        {
            if (page > 1)
            {
                --page;
                ShowPage();
            }
        }

        public void pageRight()
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
            pageText.text = page.ToString();

            for (var i = 0; i < maxSaveEntry; ++i)
            {
                var saveEntry = saveEntries[i];
                var saveEntryController = saveEntry.GetComponent<SaveEntryController>();
                var saveId = (page - 1) * maxSaveEntry + i + 1;

                string newIdText;
                string newHeaderText;
                string newFooterText;
                UnityAction onThumbnailButtonClicked;
                UnityAction onEditButtonClicked;
                UnityAction onDeleteButtonClicked;
                Sprite newThumbnailSprite;
                if (gameState.checkpointManager.UsedSaveSlots.Contains(saveId))
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