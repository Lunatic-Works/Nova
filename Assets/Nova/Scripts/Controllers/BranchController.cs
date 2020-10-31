using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class BranchController : MonoBehaviour, IRestorable
    {
        public GameObject branchButtonPrefab;
        public GameObject backPanel;

        private GameState gameState;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            gameState.BranchOccurs += OnBranchHappen;
            gameState.AddRestorable(this);
        }

        private void OnDestroy()
        {
            gameState.BranchOccurs -= OnBranchHappen;
            gameState.RemoveRestorable(this);
        }

        /// <summary>
        /// Show branch buttons when branch happens
        /// </summary>
        /// <param name="branchOccursData"></param>
        private void OnBranchHappen(BranchOccursData branchOccursData)
        {
            if (backPanel != null)
            {
                backPanel.SetActive(true);
            }

            var branchInformations = branchOccursData.branchInformations;
            foreach (var branchInformation in branchInformations)
            {
                var childButton = Instantiate(branchButtonPrefab, transform);

                var text = childButton.GetComponent<Text>();
                if (text == null)
                {
                    text = childButton.GetComponentInChildren<Text>();
                }

                text.text = branchInformation.name;

                childButton.GetComponent<Button>().onClick.AddListener(() => Select(branchInformation.name));
            }
        }

        /// <summary>
        /// Select a branch with the given name
        /// </summary>
        /// <param name="branchName">the name of the branch to select</param>
        private void Select(string branchName)
        {
            if (backPanel != null)
            {
                backPanel.SetActive(false);
            }

            gameState.SelectBranch(branchName);
            RemoveAllSelectButton();
        }

        private void RemoveAllSelectButton()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        public string restorableObjectName => "BranchController";

        public IRestoreData GetRestoreData()
        {
            return null;
        }

        public void Restore(IRestoreData restoreData)
        {
            if (backPanel != null)
            {
                backPanel.SetActive(false);
            }

            RemoveAllSelectButton();
        }
    }
}