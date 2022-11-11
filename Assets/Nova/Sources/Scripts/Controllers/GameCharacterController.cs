using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    using VoiceEntries = Dictionary<string, VoiceEntry>;

    [Serializable]
    public class VoiceEntry
    {
        public readonly string voiceFileName;
        public readonly float voiceDelay;

        public VoiceEntry(string voiceFileName, float voiceDelay)
        {
            this.voiceFileName = voiceFileName;
            this.voiceDelay = voiceDelay;
        }
    }

    [ExportCustomType]
    public class CharacterColor
    {
        [ExportCustomType]
        public enum Type
        {
            Base,
            Environment
        }

        private readonly GameCharacterController character;
        private readonly Type type;

        public CharacterColor(GameCharacterController character, Type type)
        {
            this.character = character;
            this.type = type;
        }

        public Color color
        {
            get
            {
                switch (type)
                {
                    case Type.Base:
                        return character.color;
                    case Type.Environment:
                        return character.environmentColor;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (type)
                {
                    case Type.Base:
                        character.color = value;
                        break;
                    case Type.Environment:
                        character.environmentColor = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    [ExportCustomType]
    [RequireComponent(typeof(AudioSource))]
    public class GameCharacterController : OverlaySpriteController
    {
        public string voiceFolder;

        private AudioSource audioSource;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();

            gameState.dialogueWillChange.AddListener(OnDialogueWillChange);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            gameState.restoreStarts.AddListener(StopVoice);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            gameState.dialogueWillChange.RemoveListener(OnDialogueWillChange);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            gameState.restoreStarts.RemoveListener(StopVoice);
        }

        #region Voice

        [HideInInspector] public bool stopVoiceOnDialogueChange;
        private bool willSaySomething;
        private float voiceDelay;

        private bool dontPlaySound => gameState.isRestoring;

        /// <summary>
        /// Stop the voice when the dialogue will change
        /// </summary>
        private void OnDialogueWillChange()
        {
            if (stopVoiceOnDialogueChange)
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

        private void SayImmediately(AudioClip clip, float delay)
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
                base.color = _color * _environmentColor;
            }
        }

        private Color _environmentColor = Color.white;

        public Color environmentColor
        {
            get => _environmentColor;
            set
            {
                _environmentColor = value;
                base.color = _color * _environmentColor;
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
            // Especially when stopVoiceOnDialogueChange is off
            StopVoiceAll();

            var voicePath = System.IO.Path.Combine(voiceFolder, voiceFileName);
            var audioClip = AssetLoader.Load<AudioClip>(voicePath);

            willSaySomething = true;
            voiceDelay = delay;
            audioSource.clip = audioClip;
            gameState.AddVoice(luaGlobalName, new VoiceEntry(voiceFileName, delay));
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

        [Serializable]
        private class GameCharacterControllerRestoreData : CompositeSpriteControllerRestoreData
        {
            public readonly Vector4Data environmentColor;
            public readonly int layer;

            public GameCharacterControllerRestoreData(CompositeSpriteControllerRestoreData baseData,
                Color environmentColor, int layer) : base(baseData)
            {
                this.environmentColor = environmentColor;
                this.layer = layer;
            }
        }

        public override IRestoreData GetRestoreData()
        {
            return new GameCharacterControllerRestoreData(base.GetRestoreData() as CompositeSpriteControllerRestoreData,
                environmentColor, layer);
        }

        public override void Restore(IRestoreData restoreData)
        {
            base.Restore(restoreData);
            var data = restoreData as GameCharacterControllerRestoreData;
            environmentColor = data.environmentColor;
            layer = data.layer;
        }

        #endregion

        #region Static fields and methods

        private static readonly Dictionary<string, GameCharacterController> Characters =
            new Dictionary<string, GameCharacterController>();

        private static void AddCharacter(string name, GameCharacterController character)
        {
            Characters.Add(name, character);
        }

        private static void RemoveCharacter(string name)
        {
            Characters.Remove(name);
        }

        // Cannot play voice if all characters speaking are muted
        public static bool CanPlayVoice(VoiceEntries voices)
        {
            if (voices == null)
            {
                return false;
            }

            foreach (var voice in voices)
            {
                if (Characters.TryGetValue(voice.Key, out var character) && character.audioSource.volume > 1e-3f)
                {
                    return true;
                }
            }

            return false;
        }

        public static void ReplayVoice(VoiceEntries voices, bool unbiasedDelay = true)
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
                var voicePath = System.IO.Path.Combine(character.voiceFolder, voiceEntry.voiceFileName);
                var clip = AssetLoader.Load<AudioClip>(voicePath);
                character.SayImmediately(clip, delay);
            }
        }

        public static float MaxVoiceDuration
        {
            get
            {
                float maxLength = 0.0f;
                foreach (var character in Characters.Values)
                {
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
            foreach (var character in Characters.Values)
            {
                character.StopVoice();
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
