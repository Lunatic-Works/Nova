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

            gameState.BranchOccurs += OnBranchOccurs;

            saveViewController.bookmarkSave.AddListener(OnBookmarkSave);
            saveViewController.bookmarkLoad.AddListener(OnBookmarkLoad);
            saveViewController.bookmarkDelete.AddListener(OnBookmarkDelete);
        }

        private void OnDestroy()
        {
            gameState.BranchOccurs -= OnBranchOccurs;
        }

        private void OnBookmarkSave(BookmarkSaveEventData bookmarkSaveEventData)
        {
            // Debug.Log("Bookmark save");
            checkpointManager.SaveBookmark(bookmarkSaveEventData.saveID, bookmarkSaveEventData.bookmark);
            saveViewController.ShowPage();
        }

        private void OnBookmarkLoad(BookmarkLoadEventData bookmarkLoadEventData)
        {
            // Debug.Log("Bookmark load");
            gameState.LoadBookmark(bookmarkLoadEventData.bookmark);
            saveViewController.Hide();
        }

        private void OnBookmarkDelete(BookmarkDeleteEventData bookmarkDeleteEventData)
        {
            // Debug.Log("Bookmark delete");
            checkpointManager.DeleteBookmark(bookmarkDeleteEventData.saveID);
            saveViewController.ShowPage();
        }

        private void OnBranchOccurs(BranchOccursData branchOccursData)
        {
            saveViewController.AutoSaveBookmark();
        }
    }
}