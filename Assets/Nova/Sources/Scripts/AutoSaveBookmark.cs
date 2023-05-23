using UnityEngine;

namespace Nova
{
    public class AutoSaveBookmark : MonoBehaviour
    {
        private GameState gameState;
        private SaveViewController saveViewController;

        private string lastSavedNodeName;

        private void Start()
        {
            gameState = Utils.FindNovaController().GameState;
            saveViewController = Utils.FindViewManager().GetController<SaveViewController>();

            gameState.choiceOccurs.AddListener(OnChoiceOccurs);
        }

        private void OnDestroy()
        {
            gameState.choiceOccurs.RemoveListener(OnChoiceOccurs);
        }

        private void OnChoiceOccurs(ChoiceOccursData choiceOccursData)
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
