using UnityEngine;

namespace Nova
{
    public class BookmarkSaveBehaviour : MonoBehaviour
    {
        private GameState gameState;
        private CheckpointManager checkpointManager;
        private SaveViewController saveViewController;

        private void Start()
        {
            gameState = Utils.FindNovaGameController().GameState;
            checkpointManager = Utils.FindNovaGameController().CheckpointManager;
            saveViewController = GetComponent<SaveViewController>();

            gameState.selectionOccurs.AddListener(OnSelectionOccurs);

            saveViewController.bookmarkSave.AddListener(OnBookmarkSave);
            saveViewController.bookmarkLoad.AddListener(OnBookmarkLoad);
            saveViewController.bookmarkDelete.AddListener(OnBookmarkDelete);
        }

        private void OnDestroy()
        {
            gameState.selectionOccurs.RemoveListener(OnSelectionOccurs);

            saveViewController.bookmarkSave.RemoveListener(OnBookmarkSave);
            saveViewController.bookmarkLoad.RemoveListener(OnBookmarkLoad);
            saveViewController.bookmarkDelete.RemoveListener(OnBookmarkDelete);
        }

        private void OnBookmarkSave(BookmarkSaveData bookmarkSaveData)
        {
            // Debug.Log("Bookmark save");
            checkpointManager.SaveBookmark(bookmarkSaveData.saveID, bookmarkSaveData.bookmark);
            saveViewController.ShowPage();
        }

        private void OnBookmarkLoad(BookmarkLoadData bookmarkLoadData)
        {
            // Debug.Log("Bookmark load");
            gameState.LoadBookmark(bookmarkLoadData.bookmark);
            saveViewController.Hide();
        }

        private void OnBookmarkDelete(BookmarkDeleteData bookmarkDeleteData)
        {
            // Debug.Log("Bookmark delete");
            checkpointManager.DeleteBookmark(bookmarkDeleteData.saveID);
            saveViewController.ShowPage();
        }

        private void OnSelectionOccurs(SelectionOccursData selectionOccursData)
        {
            saveViewController.AutoSaveBookmark();
        }
    }
}