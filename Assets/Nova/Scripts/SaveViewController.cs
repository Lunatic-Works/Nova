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
        public int maxPage = 3;
        public int maxSaveEntry = 9;
        public int saveEntryPerRow = 3;

        private GameObject savePanel;
        private Button leftButton;
        private Button rightButton;
        private Text pageText;
        private int page = 1;
        private readonly List<GameObject> saveEntries = new List<GameObject>();
        private SaveViewMode saveViewMode;

        /// <summary>
        /// The name of the current flow chart node
        /// </summary>
        private string currentNodeName;

        /// <summary>
        /// The index of the current dialogue entry in the current node
        /// </summary>
        private int currentDialogueIndex;

        private void Awake()
        {
            savePanel = transform.Find("SavePanel").gameObject;

            var pager = savePanel.transform.Find("Background/Right/Bottom/Pager").gameObject;
            leftButton = pager.transform.Find("LeftButton").gameObject.GetComponent<Button>();
            rightButton = pager.transform.Find("RightButton").gameObject.GetComponent<Button>();
            pageText = pager.transform.Find("PageText").gameObject.GetComponent<Text>();

            leftButton.onClick.AddListener(() => pageLeft());
            rightButton.onClick.AddListener(() => pageRight());

            var saveEntryGrid = savePanel.transform.Find("Background/Right/Top").gameObject;
            for (var i = 0; i < maxSaveEntry; ++i)
            {
                var saveEntry = saveEntryGrid.transform.Find(
                    string.Format("Row{0}/SaveEntry{1}", i / saveEntryPerRow, i % saveEntryPerRow)
                    ).gameObject;
                saveEntries.Add(saveEntry);
            }

            gameState.DialogueChanged.AddListener(OnDialogueChanged);
        }

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
                if (gameState.checkpointManager.UsedSaveSlots.Contains(saveId))
                {
                    var saveIdString = saveId.ToString();
                    newIdText = "#" + saveIdString;
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
                }
                else // Bookmark with this saveId is not found
                {
                    var saveIdString = saveId.ToString();
                    newIdText = "#" + saveIdString;
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
                }

                saveEntryController.Init(newIdText, newHeaderText, newFooterText,
                    onThumbnailButtonClicked, onEditButtonClicked, onDeleteButtonClicked);
            }
        }
    }
}