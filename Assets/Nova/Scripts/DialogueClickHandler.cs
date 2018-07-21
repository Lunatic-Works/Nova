using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;
using UnityEngine.EventSystems;

public class DialogueClickHandler : MonoBehaviour, IPointerClickHandler
{
    public GameState gameState;

    private bool isAnimating = false;
    private DialogueBoxController dialogueBoxController;

    private void Start()
    {
        dialogueBoxController = GetComponentInChildren<DialogueBoxController>();
    }

    public void DialogueBoxStartAnimation()
    {
        isAnimating = true;
    }

    public void DialogueBoxEndAnimation()
    {
        isAnimating = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnimating)
        {
            dialogueBoxController.StopCharacterAnimation();
            return;
        }

        gameState.Step();
    }
}