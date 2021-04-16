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

        private IEnumerable<IAbstractKeyDevice> keyDevices
        {
            get { yield return keyboard; }
        }

        private string GetMappingFileOrDefault(string path, TextAsset defaultMapping)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception)
            {
                if (defaultMapping != null)
                {
                    return defaultMapping.text;
                }
            }

            return null;
        }

        private void LoadKeyBoard()
        {
            keyboard = new AbstractKeyboard();
            try
            {
                keyboard.Load(File.ReadAllText(KeyBoardMappingFilePath));
            }
            catch
            {
                Debug.LogWarning("Nova: Failed to load input mapping file, use default input mapping.");
                keyboard.Load(defaultKeyboardMapping.text);
            }

            // load default keyboard
            defaultKeyboard = new AbstractKeyboard();
            defaultKeyboard.Load(defaultKeyboardMapping.text);
        }

        private void Load()
        {
            LoadKeyBoard();
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
            Load();
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