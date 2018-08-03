﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class CharacterController : MonoBehaviour, IRestorable
    {
        public string characterVariableName;

        public string voiceFileFolder;

        public GameState gameState;

        private AudioSource audioSource;

        private GameObject characterAppearance;

        private void Start()
        {
            LuaRuntime.Instance.BindObject(characterVariableName, this, "_G");
            audioSource = GetComponent<AudioSource>();
            gameState.DialogueChanged.AddListener(OnDialogueChanged);
            gameState.DialogueWillChange.AddListener(OnDialogueWillChange);
            characterAppearance = transform.Find("Appearance").gameObject;
            gameState.AddRestorable(this);
        }

        private bool willSaySomething = false;

        /// <summary>
        /// Stop the voice when the dialogue will change
        /// </summary>
        private void OnDialogueWillChange()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Play the voice when the dialogue actually changes
        /// </summary>
        /// <param name="dialogueChangedEventData"></param>
        private void OnDialogueChanged(DialogueChangedEventData dialogueChangedEventData)
        {
            if (willSaySomething)
            {
                audioSource.Play();
            }

            willSaySomething = false;
        }

        #region Methods called by external scripts

        /// <summary>
        /// Make the character say something
        /// </summary>
        /// <remarks>
        /// Character will not say something immediately after this method is called. it will be marked as what to
        /// say something and speaks when the dialogue really changed
        /// </remarks>
        /// <param name="voiceFileName"></param>
        public void Say(string voiceFileName)
        {
            voiceFileName = voiceFileFolder + voiceFileName;
            var audio = AssetsLoader.GetAudioClip(voiceFileName);
            // A character has only one mouth
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            willSaySomething = true;
            audioSource.clip = audio;
            gameState.AddVoiceClipOfNextDialogue(voiceFileName);
        }

        /// <summary>
        /// Make the character stop speaking
        /// </summary>
        public void StopVoice()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Show the character
        /// </summary>
        public void Show()
        {
            characterAppearance.SetActive(true);
        }

        /// <summary>
        /// Hide the character
        /// </summary>
        public void Hide()
        {
            characterAppearance.SetActive(false);
        }

        #endregion

        [Serializable]
        private class RestoreData : IRestoreData
        {
            public bool isActive { get; private set; }

            public RestoreData(bool isActive)
            {
                this.isActive = isActive;
            }
        }

        public string restorableObjectName
        {
            get { return characterVariableName; }
        }

        public IRestoreData GetRestoreData()
        {
            return new RestoreData(characterAppearance.activeSelf);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as RestoreData;
            characterAppearance.SetActive(data.isActive);
        }
    }
}