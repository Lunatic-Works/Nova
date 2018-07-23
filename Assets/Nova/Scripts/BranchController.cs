using System;
using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class BranchController : MonoBehaviour
    {
        public Button branchButtomPrefab;

        public GameState gameState;

        /// <summary>
        /// Show branch buttons when branch happens
        /// </summary>
        /// <param name="branchOccursEventData"></param>
        public void OnBranchHappen(BranchOccursEventData branchOccursEventData)
        {
            var branchInformations = branchOccursEventData.branchInformations;
            foreach (var branchInformation in branchInformations)
            {
                var childButtom = Instantiate(branchButtomPrefab);
                childButtom.transform.SetParent(transform);
                var text = childButtom.GetComponent<Text>();
                if (text == null)
                {
                    text = childButtom.GetComponentInChildren<Text>();
                }

                text.text = branchInformation.name;
                childButtom.onClick.AddListener(() => Select(branchInformation.name));
            }
        }

        /// <summary>
        /// Select a branch with the given name
        /// </summary>
        /// <param name="branchName">the name of the branch to select</param>
        private void Select(string branchName)
        {
            gameState.SelectBranch(branchName);
            foreach (Transform child in transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }
}