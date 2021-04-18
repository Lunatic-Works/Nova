using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class InputMapper : MonoBehaviour
    {
        public TextAsset defaultKeyboardMapping;

        private Dictionary<AbstractKey, bool> keyStatus = new Dictionary<AbstractKey, bool>();
        private Dictionary<AbstractKey, bool> keyStatusLastFrame = new Dictionary<AbstractKey, bool>();
        private readonly Dictionary<AbstractKey, bool> keyEnabled = new Dictionary<AbstractKey, bool>();

        public static string InputFilesDirectory => Path.Combine(Application.persistentDataPath, "Input");
        private static string KeyBoardMappingFilePath => Path.Combine(InputFilesDirectory, "keyboard.json");

        public void SetEnable(AbstractKey key, bool value)
        {
            keyEnabled[key] = value;
        }

        public bool IsEnabled(AbstractKey key)
        {
            return keyEnabled[key];
        }

        public void SetEnableAll(bool value)
        {
            foreach (AbstractKey key in Enum.GetValues(typeof(AbstractKey)))
            {
                SetEnable(key, value);
            }
        }

        public bool IsEnabledAll()
        {
            return Enum.GetValues(typeof(AbstractKey)).Cast<AbstractKey>().All(key => keyEnabled[key]);
        }

        public AbstractKeyboard keyboard { get; private set; }

        private AbstractKeyboard defaultKeyboard;

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

        private void LoadKeyBoard()
        {
            defaultKeyboard = new AbstractKeyboard();
            defaultKeyboard.Load(defaultKeyboardMapping.text);

            keyboard = new AbstractKeyboard();
            try
            {
                keyboard.Load(File.ReadAllText(KeyBoardMappingFilePath));

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

        private void SaveKeyBoard()
        {
            if (keyboard == null)
            {
                return;
            }

            File.WriteAllText(KeyBoardMappingFilePath, keyboard.Json());
        }

        public void Save()
        {
            if (!Directory.Exists(InputFilesDirectory))
            {
                Directory.CreateDirectory(InputFilesDirectory);
            }

            SaveKeyBoard();
        }

        private void Awake()
        {
            LoadKeyBoard();
            foreach (AbstractKey key in Enum.GetValues(typeof(AbstractKey)))
            {
                keyStatus[key] = false;
                keyStatusLastFrame[key] = false;
            }

            SetEnableAll(true);
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
            }
        }
    }
}