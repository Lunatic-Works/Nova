using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class BranchController : MonoBehaviour, IRestorable
    {
        public BranchButtonController branchButtonPrefab;
        public GameObject backPanel;
        public string imageFolder;

        private GameState gameState;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            gameState.BranchOccurs += OnBranchOccurs;
            gameState.AddRestorable(this);
        }

        private void OnDestroy()
        {
            gameState.BranchOccurs -= OnBranchOccurs;
            gameState.RemoveRestorable(this);
        }

        /// <summary>
        /// Show branch buttons when branch happens
        /// </summary>
        /// <param name="branchOccursData"></param>
        private void OnBranchOccurs(BranchOccursData branchOccursData)
        {
            var branchInformations = branchOccursData.branchInformations.ToList();

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

                var button = Instantiate(branchButtonPrefab, transform);
                button.Init(branchInfo.texts, branchInfo.imageInfo, imageFolder, () => Select(branchInfo.name),
                    interactable: branchInfo.mode != BranchMode.Enable || branchInfo.condition.Invoke<bool>());
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

            RemoveAllSelectButton();
            gameState.SelectBranch(branchName);
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