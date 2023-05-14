using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class ChoicesController : MonoBehaviour
    {
        [SerializeField] private ChoiceButtonController choiceButtonPrefab;
        [SerializeField] private GameObject backPanel;
        [SerializeField] private string imageFolder;

        private GameState gameState;
        [HideInInspector] public int activeChoiceCount;

        private void Awake()
        {
            RemoveAllChoices();

            gameState = Utils.FindNovaController().GameState;
            gameState.choiceOccurs.AddListener(OnChoiceOccurs);
            gameState.restoreStarts.AddListener(OnRestoreStarts);
        }

        private void OnDestroy()
        {
            gameState.choiceOccurs.RemoveListener(OnChoiceOccurs);
            gameState.restoreStarts.RemoveListener(OnRestoreStarts);
        }

        private void OnChoiceOccurs(ChoiceOccursData data)
        {
            RaiseChoices(data.choices);
        }

        public void RaiseChoices(IReadOnlyList<ChoiceOccursData.Choice> choices)
        {
            if (choices.Count == 0)
            {
                throw new ArgumentException("Nova: No active selection.");
            }

            if (backPanel != null)
            {
                backPanel.SetActive(true);
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var index = i;
                var button = Instantiate(choiceButtonPrefab, transform);
                // Prevent showing the button before init
                button.gameObject.SetActive(false);
                button.Init(choice.texts, choice.imageInfo, imageFolder, () => Select(index),
                    choice.interactable);
                button.gameObject.SetActive(true);
            }

            activeChoiceCount = choices.Count;
        }

        public void Select(int index)
        {
            RemoveAllChoices();
            gameState.SignalFence(index);
        }

        private void OnRestoreStarts(bool isInitial)
        {
            RemoveAllChoices();
        }

        private void RemoveAllChoices()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            activeChoiceCount = 0;

            if (backPanel != null)
            {
                backPanel.SetActive(false);
            }
        }
    }
}
