// TODO
// Call GameState's save and load functions
// Show thumbnail
// Show save date and time
// Infinite pages, calculate maxPage from UsedSaveSlots

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
        public int maxPage;
        public int maxSaveEntry;

        private GameObject savePanel;
        private GameObject saveContent;
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
            saveContent = savePanel.transform.Find("SaveEntries").gameObject;
            leftButton = savePanel.transform.Find("Pager/LeftButton").gameObject.GetComponent<Button>();
            rightButton = savePanel.transform.Find("Pager/RightButton").gameObject.GetComponent<Button>();
            pageText = savePanel.transform.Find("Pager/PageText").gameObject.GetComponent<Text>();

            for (var i = 0; i < maxSaveEntry; ++i)
            {
                var saveEntry = Instantiate(SaveEntryPrefab);

                saveEntries.Add(saveEntry);
                saveEntry.transform.SetParent(saveContent.transform);
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

                string newText;
                UnityAction onButtonClicked;
                UnityAction onDeleteButtonClicked;
                if (gameState.checkpointManager.UsedSaveSlots.Contains(saveId))
                {
                    newText = saveId.ToString() + " found";

                    if (saveViewMode == SaveViewMode.Save)
                    {
                        onButtonClicked = () => SaveBookmark(saveId);
                        onDeleteButtonClicked = () => DeleteBookmark(saveId);
                    }
                    else // saveViewMode == SaveViewMode.Load
                    {
                        onButtonClicked = () => LoadBookmark(saveId);
                        onDeleteButtonClicked = () => DeleteBookmark(saveId);
                    }
                }
                else // Bookmark with this saveId is not found
                {
                    newText = saveId.ToString();

                    if (saveViewMode == SaveViewMode.Save)
                    {
                        onButtonClicked = () => SaveBookmark(saveId);
                        onDeleteButtonClicked = null;
                    }
                    else // saveViewMode == SaveViewMode.Load
                    {
                        onButtonClicked = null;
                        onDeleteButtonClicked = null;
                    }
                }

                saveEntryController.Init(newText, onButtonClicked, onDeleteButtonClicked);
            }
        }
    }
}