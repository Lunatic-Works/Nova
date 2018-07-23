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
            text = transform.Find("Text").gameObject.GetComponent<Text>();
            var buttons = transform.Find("Text/Buttons");
            goBackButton = buttons.Find("GoBackButton").gameObject.GetComponent<Button>();
            playVoiceButton = buttons.Find("ReplayVoiceButton").gameObject.GetComponent<Button>();
            addFavoriteButton = buttons.Find("AddFavoriteButton").gameObject.GetComponent<Button>();
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
            goBackButton.onClick.AddListener(onGoBackButtonClicked);
            playVoiceButton.onClick.AddListener(onPlayVoiceButtonClicked);
            addFavoriteButton.onClick.AddListener(onAddFavoriteButtonClicked);
        }
    }
}