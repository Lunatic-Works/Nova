using UnityEngine;

namespace Nova
{
    public class BookmarkSaveBehaviour : MonoBehaviour
    {
        private GameState gameState;
        private SaveViewController saveViewController;

        private string lastSavedNodeName;

        private void Start()
        {
            gameState = Utils.FindNovaController().GameState;
            saveViewController = Utils.FindViewManager().GetController<SaveViewController>();

            gameState.selectionOccurs.AddListener(OnSelectionOccurs);
        }

        private void OnDestroy()
        {
            gameState.selectionOccurs.RemoveListener(OnSelectionOccurs);
        }

        private void OnSelectionOccurs(SelectionOccursData selectionOccursData)
        {
            if (gameState.currentNode.name == lastSavedNodeName)
            {
                return;
            }

            saveViewController.AutoSaveBookmark();
            lastSavedNodeName = gameState.currentNode.name;
        }
    }
}
