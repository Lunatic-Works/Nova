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
            var branchInformations = branchOccursData.branchInformations;

            foreach (var branchInfo in branchInformations)
            {
                if (branchInfo.mode == BranchMode.Jump)
                {
                    if (branchInfo.condition == null || branchInfo.condition.Invoke<bool>())
                    {
                        gameState.SelectBranch(branchInfo.name);
                        return;
                    }
                }
            }

            if (backPanel != null)
            {
                backPanel.SetActive(true);
            }

            foreach (var branchInfo in branchInformations)
            {
                if (branchInfo.mode == BranchMode.Jump)
                {
                    continue;
                }

                if (branchInfo.mode == BranchMode.Show && !branchInfo.condition.Invoke<bool>())
                {
                    continue;
                }

                var child = Instantiate(branchButtonPrefab, transform);

                var text = child.GetComponentInChildren<Text>();
                text.text = branchInfo.text;

                var button = child.GetComponentInChildren<Button>();
                button.onClick.AddListener(() => Select(branchInfo.name));
                if (branchInfo.mode == BranchMode.Enable)
                {
                    button.interactable = branchInfo.condition.Invoke<bool>();
                }
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