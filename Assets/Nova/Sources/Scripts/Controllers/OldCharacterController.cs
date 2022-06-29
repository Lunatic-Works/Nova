﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    [RequireComponent(typeof(AudioSource))]
    public class OldCharacterController : CompositeSpriteControllerBase
    {
        public string luaGlobalName;
        public string voiceFolder;

        private AudioSource audioSource;

        public int layer
        {
            get
            {
                if (textureChanger != null)
                {
                    var gameOverlayTextureChanger = textureChanger as GameOverlayTextureChanger;
                    if (gameOverlayTextureChanger.actualImageObject != null)
                    {
                        return gameOverlayTextureChanger.actualImageObject.layer;
                    }
                    else
                    {
                        return gameObject.layer;
                    }
                }
                else
                {
                    return gameObject.layer;
                }
            }
            set
            {
                if (textureChanger != null)
                {
                    var gameOverlayTextureChanger = textureChanger as GameOverlayTextureChanger;
                    if (gameOverlayTextureChanger.actualImageObject != null)
                    {
                        gameOverlayTextureChanger.actualImageObject.layer = value;
                    }
                    else
                    {
                        gameObject.layer = value;
                    }
                }
                else
                {
                    gameObject.layer = value;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();

            gameState.dialogueWillChange.AddListener(OnDialogueWillChange);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            gameState.dialogueWillChange.RemoveListener(OnDialogueWillChange);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        #region Voice

        [HideInInspector] public bool stopVoiceWhenDialogueWillChange;

        private bool willSaySomething = false;
        private float voiceDelay = 0.0f;

        private bool dontPlaySound => gameState.isRestoring;

        /// <summary>
        /// Stop the voice when the dialogue will change
        /// </summary>
        private void OnDialogueWillChange(DialogueWillChangeData dialogueWillChangeData)
        {
            if (stopVoiceWhenDialogueWillChange)
            {
                audioSource.Stop();
            }

            if (!audioSource.isPlaying)
            {
                audioSource.clip = null;
            }

            // reset status
            willSaySomething = false;
            voiceDelay = 0.0f;
        }

        /// <summary>
        /// Play the voice when the dialogue actually changes
        /// </summary>
        /// <param name="dialogueChangedData"></param>
        private void OnDialogueChanged(DialogueChangedData dialogueChangedData)
        {
            if (willSaySomething && !dontPlaySound)
            {
                audioSource.PlayDelayed(voiceDelay);
            }
        }

        private void SayImmediatelyWithDelay(AudioClip clip, float delay)
        {
            StopVoice();
            audioSource.clip = clip;
            audioSource.PlayDelayed(delay);
        }

        #endregion

        #region Color

        private Color _color = Color.white;

        public override Color color
        {
            get => _color;
            set
            {
                _color = value;
                SetColor(_color * _environmentColor);
            }
        }

        private Color _environmentColor = Color.white;

        public Color environmentColor
        {
            get => _environmentColor;
            set
            {
                _environmentColor = value;
                SetColor(_color * _environmentColor);
            }
        }

        #endregion

        #region Methods called by external scripts

        /// <summary>
        /// Make the character say something
        /// </summary>
        /// <remarks>
        /// Character will not say something immediately after this method is called. it will be marked as what to
        /// say something and speaks when the dialogue really changed
        /// </remarks>
        /// <param name="voiceFileName"></param>
        /// <param name="delay">the delay for this voice</param>
        public void Say(string voiceFileName, float delay)
        {
            // Stop all to make sure all previous playing voices are stopped here
            // Especially when stopVoiceWhenDialogueWillChange is off
            StopVoiceAll();

            voiceFileName = System.IO.Path.Combine(voiceFolder, voiceFileName);
            var audioClip = AssetLoader.Load<AudioClip>(voiceFileName);

            willSaySomething = true;
            voiceDelay = delay;
            audioSource.clip = audioClip;
            gameState.AddVoiceNextDialogue(luaGlobalName, new VoiceEntry(voiceFileName, delay));
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

        #endregion

        #region Restoration

        public override string restorableName => luaGlobalName;

        [Serializable]
        private class CharacterControllerRestoreData : CompositeSpriteControllerBaseRestoreData
        {
            public readonly Vector4Data environmentColor;
            public readonly int layer;

            public CharacterControllerRestoreData(CompositeSpriteControllerBaseRestoreData baseData,
                Color environmentColor, int layer) : base(baseData)
            {
                this.environmentColor = environmentColor;
                this.layer = layer;
            }
        }

        public override IRestoreData GetRestoreData()
        {
            return new CharacterControllerRestoreData(base.GetRestoreData() as CompositeSpriteControllerBaseRestoreData,
                environmentColor, layer);
        }

        public override void Restore(IRestoreData restoreData)
        {
            base.Restore(restoreData);
            var data = restoreData as CharacterControllerRestoreData;
            environmentColor = data.environmentColor;
            layer = data.layer;
        }

        #endregion

        #region Static fields and methods

        private static readonly Dictionary<string, OldCharacterController> Characters =
            new Dictionary<string, OldCharacterController>();

        private static void AddCharacter(string name, OldCharacterController character)
        {
            Characters.Add(name, character);
        }

        private static void RemoveCharacter(string name)
        {
            Characters.Remove(name);
        }

        public static void ReplayVoice(IReadOnlyDictionary<string, VoiceEntry> voices, bool unbiasedDelay = true)
        {
            StopVoiceAll();
            float bias = 0.0f;
            if (unbiasedDelay)
            {
                bias = voices.Values.Select(v => v.voiceDelay).Min();
            }

            foreach (var voice in voices)
            {
                string characterName = voice.Key;
                VoiceEntry voiceEntry = voice.Value;
                float delay = voiceEntry.voiceDelay;
                if (unbiasedDelay)
                {
                    delay -= bias;
                }

                if (!Characters.TryGetValue(characterName, out var character)) continue;
                var clip = AssetLoader.Load<AudioClip>(voiceEntry.voiceFileName);
                character.SayImmediatelyWithDelay(clip, delay);
            }
        }

        public static float MaxVoiceDurationNextDialogue
        {
            get
            {
                float maxLength = 0.0f;
                foreach (var c in Characters)
                {
                    var character = c.Value;
                    if (!character.willSaySomething) continue;
                    var clip = character.audioSource.clip;
                    if (clip == null) continue;
                    float length = clip.length + character.voiceDelay;
                    if (length > maxLength)
                    {
                        maxLength = length;
                    }
                }

                return maxLength;
            }
        }

        public static void StopVoiceAll()
        {
            foreach (var c in Characters)
            {
                c.Value.StopVoice();
            }
        }

        #endregion

        private void OnEnable()
        {
            AddCharacter(luaGlobalName, this);
        }

        private void OnDisable()
        {
            RemoveCharacter(luaGlobalName);
        }
    }
}