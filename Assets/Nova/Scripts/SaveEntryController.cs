using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class SaveEntryController : MonoBehaviour
    {
        private Button button;
        private Button deleteButton;
        private Text text;

        private void Awake()
        {
            button = transform.Find("Button").gameObject.GetComponent<Button>();
            deleteButton = transform.Find("DeleteButton").gameObject.GetComponent<Button>();
            text = button.transform.Find("Text").gameObject.GetComponent<Text>();
        }

        private void InitButton(Button button, UnityAction onClickAction)
        {
            if (onClickAction == null)
            {
                button.gameObject.SetActive(false);
                return;
            }
            else
            {
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClickAction);
            }
        }

        public void Init(string newText, UnityAction onButtonClicked, UnityAction onDeleteButtonClicked)
        {
            text.text = newText;
            InitButton(button, onButtonClicked);
            InitButton(deleteButton, onDeleteButtonClicked);
        }
    }
}