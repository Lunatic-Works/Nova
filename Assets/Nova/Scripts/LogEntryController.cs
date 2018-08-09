using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class LogEntryController : MonoBehaviour
    {
        private Text text;
        private Button goBackButton;
        private Button playVoiceButton;
        private Button addFavoriteButton;

        private void Awake()
        {
            text = transform.Find("Text").GetComponent<Text>();
            var buttons = text.transform.Find("Buttons");
            goBackButton = buttons.Find("GoBackButton").GetComponent<Button>();
            playVoiceButton = buttons.Find("PlayVoiceButton").GetComponent<Button>();
            addFavoriteButton = buttons.Find("AddFavoriteButton").GetComponent<Button>();
        }

        private void InitButton(Button button, UnityAction onClickAction)
        {
            if (onClickAction == null)
            {
                button.gameObject.SetActive(false);
                return;
            }

            button.onClick.AddListener(onClickAction);
        }

        /// <summary>
        /// Initialize the log entry prefab
        /// </summary>
        /// <param name="logEntryText">The text to be displayed on the log entry</param>
        /// <param name="onGoBackButtonClicked">The action to perform when the go back button clicked</param>
        /// <param name="onPlayVoiceButtonClicked">The action to perform when the play voice button clicked</param>
        /// <param name="onAddFavoriteButtonClicked">The action to perform when the add favorite button clicked</param>
        public void Init(string logEntryText, UnityAction onGoBackButtonClicked, UnityAction onPlayVoiceButtonClicked,
            UnityAction onAddFavoriteButtonClicked)
        {
            text.text = logEntryText;
            InitButton(goBackButton, onGoBackButtonClicked);
            InitButton(playVoiceButton, onPlayVoiceButtonClicked);
            InitButton(addFavoriteButton, onAddFavoriteButtonClicked);
        }
    }
}