using UnityEngine;

namespace Nova
{
    public class AutoSaveBookmark : MonoBehaviour
    {
        private GameState gameState;
        private SaveViewController saveViewController;

        private string lastSavedNodeName;
        private int lastSavedDialogueIndex;

        private void Start()
        {
            gameState = Utils.FindNovaController().GameState;
            saveViewController = Utils.FindViewManager().GetController<SaveViewController>();

            gameState.choiceOccurs.AddListener(Save);
            gameState.routeEnded.AddListener(Save);
            saveViewController.bookmarkSaved.AddListener(UpdateSaved);
            saveViewController.bookmarkLoaded.AddListener(UpdateSaved);
        }

        private void OnDestroy()
        {
            gameState.choiceOccurs.RemoveListener(Save);
            gameState.routeEnded.RemoveListener(Save);
            saveViewController.bookmarkSaved.RemoveListener(UpdateSaved);
            saveViewController.bookmarkLoaded.RemoveListener(UpdateSaved);
        }

        private void Save(GameStateEventData _)
        {
            if (gameState.currentNode.name == lastSavedNodeName && gameState.currentIndex == lastSavedDialogueIndex)
            {
                return;
            }

            saveViewController.AutoSaveBookmark();
        }

        private void UpdateSaved()
        {
            lastSavedNodeName = gameState.currentNode.name;
            lastSavedDialogueIndex = gameState.currentIndex;
        }
    }
}
