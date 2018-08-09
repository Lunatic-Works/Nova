using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class TestBookmarkSaveLoadDelete : MonoBehaviour
    {
        public SaveViewController saveViewController;

        private GameState gameState;
        private CheckpointManager checkpointManager;

        void Start()
        {
            gameState = GameState.Instance;
            checkpointManager = Utils.FindGameController().GetComponent<CheckpointManager>();

            saveViewController.BookmarkSave.AddListener(OnBookmarkSave);
            saveViewController.BookmarkLoad.AddListener(OnBookmarkLoad);
            saveViewController.BookmarkDelete.AddListener(OnBookmarkDelete);
        }

        private void OnBookmarkSave(BookmarkSaveEventData bookmarkSaveEventData)
        {
            Debug.Log("Bookmark save");
            checkpointManager.SaveBookmark(bookmarkSaveEventData.saveId, bookmarkSaveEventData.bookmark);
            saveViewController.Hide();
        }

        private void OnBookmarkLoad(BookmarkLoadEventData bookmarkLoadEventData)
        {
            Debug.Log("Bookmark load");
            gameState.LoadBookmark(bookmarkLoadEventData.bookmark);
            saveViewController.Hide();
        }

        private void OnBookmarkDelete(BookmarkDeleteEventData bookmarkDeleteEventData)
        {
            Debug.Log("Bookmark delete");
            checkpointManager.DeleteBookmark(bookmarkDeleteEventData.saveId);
            saveViewController.ShowPage();
        }
    }
}