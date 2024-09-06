using UnityEngine;

namespace Nova
{
    public class AutoSaveBookmark : MonoBehaviour
    {
        public static AutoSaveBookmark Current;

        private GameState gameState;
        private SaveViewController saveViewController;

        private string lastSavedNodeName;
        private int lastSavedDialogueIndex;

        private void Awake()
        {
            Current = this;
            gameState = Utils.FindNovaController().GameState;
            saveViewController = Utils.FindViewManager().GetController<SaveViewController>();

            gameState.choiceOccurs.AddListener(OnEvent);
            gameState.routeEnded.AddListener(OnEvent);
            saveViewController.bookmarkSaved.AddListener(UpdateSaved);
            saveViewController.bookmarkLoaded.AddListener(UpdateSaved);
        }

        private void OnDestroy()
        {
            gameState.choiceOccurs.RemoveListener(OnEvent);
            gameState.routeEnded.RemoveListener(OnEvent);
            saveViewController.bookmarkSaved.RemoveListener(UpdateSaved);
            saveViewController.bookmarkLoaded.RemoveListener(UpdateSaved);
        }

        private void OnEvent(GameStateEventData _)
        {
            TrySave();
        }

        private void UpdateSaved()
        {
            lastSavedNodeName = gameState.currentNode.name;
            lastSavedDialogueIndex = gameState.currentIndex;
        }

        private bool isSame => gameState.currentNode.name == lastSavedNodeName &&
                               gameState.currentIndex == lastSavedDialogueIndex;

        public void TrySave(Texture2D screenshot)
        {
            if (isSame)
            {
                return;
            }

            saveViewController.AutoSaveBookmark(screenshot);
        }

        public void TrySave()
        {
            if (isSame)
            {
                return;
            }

            saveViewController.AutoSaveBookmark();
        }
    }
}
