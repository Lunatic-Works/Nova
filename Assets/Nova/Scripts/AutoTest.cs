using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class AutoTest : MonoBehaviour
    {
        public DialogueBoxController dialogueBoxController;

        void Start()
        {
            dialogueBoxController.AutoModeStarts.AddListener(OnAutoModeStarts);
            dialogueBoxController.AutoModeStops.AddListener(OnAutoModeEnds);
        }

        private void OnAutoModeStarts()
        {
            Debug.Log("Auto Start");
           // dialogueBoxController.State = DialogueBoxState.Normal;
        }

        private void OnAutoModeEnds()
        {
            Debug.Log("Auto End");
        }
    }
}