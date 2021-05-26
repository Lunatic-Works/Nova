using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Nova
{
    using KeyStatus = Dictionary<AbstractKey, bool>;
    using AbstractKeyGroups = Dictionary<AbstractKey, AbstractKeyGroup>;
    using AbstractKeyTags = Dictionary<AbstractKey, bool>;

    public class InputMapper : MonoBehaviour
    {
        public static string InputFilesDirectory => Path.Combine(Application.persistentDataPath, "Input");

        private static string KeyboardMappingFilePath => Path.Combine(InputFilesDirectory, "keyboard.json");

        public TextAsset defaultKeyboardMapping;

        private KeyStatus keyStatus = new KeyStatus();
        private KeyStatus keyStatusLastFrame = new KeyStatus();
        private KeyStatus keyEnabled = new KeyStatus();
        private readonly KeyStatus keyDownWhenEnabled = new KeyStatus();
        private readonly KeyStatus keyDownToBeCleared = new KeyStatus();

        public readonly AbstractKeyboard keyboard = new AbstractKeyboard();
        public readonly AbstractKeyGroups keyGroups = new AbstractKeyGroups();
        public readonly AbstractKeyTags keyIsEditor = new AbstractKeyTags();

        private readonly AbstractKeyboard defaultKeyboard = new AbstractKeyboard();

        #region Enable

        public void SetEnable(AbstractKey key, bool value)
        {
            keyEnabled[key] = value;
            keyDownWhenEnabled[key] = false;
            keyDownToBeCleared[key] = false;
        }

        public bool IsEnabled(AbstractKey key)
        {
#if !UNITY_EDITOR
            if (keyIsEditor[key])
            {
                return false;
            }
#endif

            return keyEnabled[key];
        }

        public void SetEnableGroup(AbstractKeyGroup group)
        {
            foreach (AbstractKey key in Enum.GetValues(typeof(AbstractKey)))
            {
                SetEnable(key, (keyGroups[key] & group) > 0);
            }
        }

        public KeyStatus GetEnabledState()
        {
            return keyEnabled.ToDictionary(x => x.Key, x => x.Value);
        }

        public void SetEnabledState(KeyStatus keyEnabled)
        {
            this.keyEnabled = keyEnabled;
        }

        #endregion

        public AbstractKeyboardData GetDefaultKeyboardData()
        {
            return defaultKeyboard.Data.GetCopy();
        }

        public List<CompoundKey> GetDefaultCompoundKeys(AbstractKey key)
        {
            return defaultKeyboard.Data[key].Select(ck => new CompoundKey(ck)).ToList();
        }

        private IEnumerable<IAbstractKeyDevice> keyDevices
        {
            get { yield return keyboard; }
        }

        #region Save and load

        private void LoadKeyboard()
        {
            defaultKeyboard.Init();
            defaultKeyboard.LoadFull(defaultKeyboardMapping.text, keyGroups, keyIsEditor);

            keyboard.Init();
            try
            {
                keyboard.Load(File.ReadAllText(KeyboardMappingFilePath));

                // Use default values to fill missing keys
                foreach (AbstractKey ak in Enum.GetValues(typeof(AbstractKey)))
                {
                    if (!keyboard.Data.ContainsKey(ak))
                    {
                        keyboard.Data[ak] = GetDefaultCompoundKeys(ak);
                    }
                }
            }
            catch
            {
                Debug.LogWarning("Nova: Failed to load input mapping file, use default input mapping.");
                keyboard.Data = GetDefaultKeyboardData();
            }
        }

        private void SaveKeyboard()
        {
            File.WriteAllText(KeyboardMappingFilePath, keyboard.Json());
        }

        public void Save()
        {
            if (!inited)
            {
                return;
            }

            if (!Directory.Exists(InputFilesDirectory))
            {
                Directory.CreateDirectory(InputFilesDirectory);
            }

            SaveKeyboard();
        }

        #endregion

        private bool inited;

        public void Init()
        {
            if (inited)
            {
                return;
            }

            LoadKeyboard();

            foreach (AbstractKey key in Enum.GetValues(typeof(AbstractKey)))
            {
                keyStatus[key] = false;
                keyStatusLastFrame[key] = false;
                keyEnabled[key] = true;
                keyDownWhenEnabled[key] = false;
                keyDownToBeCleared[key] = false;
            }

            inited = true;
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            Save();
        }

        public bool GetKey(AbstractKey key)
        {
            if (!IsEnabled(key)) return false;
            return keyStatus[key];
        }

        public bool GetKeyDown(AbstractKey key)
        {
            if (!IsEnabled(key)) return false;
            return !keyStatusLastFrame[key] && keyStatus[key];
        }

        public bool GetKeyUp(AbstractKey key)
        {
            if (!IsEnabled(key)) return false;
            if (!keyDownWhenEnabled[key]) return false;
            return keyStatusLastFrame[key] && !keyStatus[key];
        }

        private void SwapKeyStatus()
        {
            var tmp = keyStatusLastFrame;
            keyStatusLastFrame = keyStatus;
            keyStatus = tmp;
        }

        private void Update()
        {
            foreach (var device in keyDevices)
            {
                device.Update();
            }

            SwapKeyStatus();

            foreach (AbstractKey key in Enum.GetValues(typeof(AbstractKey)))
            {
                keyStatus[key] = keyDevices.Any(device => device.GetKey(key));

                if (keyDownToBeCleared[key])
                {
                    keyDownToBeCleared[key] = false;
                    keyDownWhenEnabled[key] = false;
                }

                if (GetKeyDown(key))
                {
                    keyDownWhenEnabled[key] = true;
                }

                if (GetKeyUp(key))
                {
                    keyDownToBeCleared[key] = true;
                }
            }
        }
    }
}