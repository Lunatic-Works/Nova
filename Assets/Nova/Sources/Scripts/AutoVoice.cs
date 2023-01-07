using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class AutoVoiceConfig
    {
        public string characterName;
        public GameCharacterController characterController;
        public string prefix;
    }

    [ExportCustomType]
    public class AutoVoice : MonoBehaviour, IRestorable
    {
        public string luaName;
        public List<AutoVoiceConfig> autoVoiceConfigs;

        /// <summary>
        /// Length of the voice file name after padding zeros to the left.
        /// </summary>
        public int padWidth;

        private GameState gameState;
        private readonly Dictionary<string, AutoVoiceConfig> nameToConfig = new Dictionary<string, AutoVoiceConfig>();
        private Dictionary<string, bool> nameToEnabled = new Dictionary<string, bool>();
        private Dictionary<string, int> nameToIndex = new Dictionary<string, int>();

        private void Awake()
        {
            gameState = Utils.FindNovaController().GameState;

            foreach (var config in autoVoiceConfigs)
            {
                var name = config.characterName;
                nameToConfig[name] = config;
                nameToEnabled[name] = false;
                nameToIndex[name] = 0;
            }

            if (!string.IsNullOrEmpty(luaName))
            {
                LuaRuntime.Instance.BindObject(luaName, this);
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        public GameCharacterController GetCharacterController(string name)
        {
            return nameToConfig[name].characterController;
        }

        public string GetAudioName(string name)
        {
            return nameToConfig[name].prefix + nameToIndex[name].ToString().PadLeft(padWidth, '0');
        }

        public bool GetEnabled(string name)
        {
            return nameToEnabled.ContainsKey(name) && nameToEnabled[name];
        }

        public void SetEnabled(string name, bool value)
        {
            nameToEnabled[name] = value;
        }

        public void DisableAll()
        {
            foreach (var name in nameToEnabled.Keys.ToList())
            {
                nameToEnabled[name] = false;
            }
        }

        public void SetIndex(string name, int value)
        {
            nameToIndex[name] = value;
        }

        public void IncrementIndex(string name)
        {
            ++nameToIndex[name];
        }

        #region Restoration

        public string restorableName => luaName;

        [Serializable]
        private class AutoVoiceRestoreData : IRestoreData
        {
            public readonly Dictionary<string, bool> nameToEnabled;
            public readonly Dictionary<string, int> nameToIndex;

            public AutoVoiceRestoreData(Dictionary<string, bool> nameToEnabled, Dictionary<string, int> nameToIndex)
            {
                this.nameToEnabled = nameToEnabled.ToDictionary(x => x.Key, x => x.Value);
                this.nameToIndex = nameToIndex.ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new AutoVoiceRestoreData(nameToEnabled, nameToIndex);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as AutoVoiceRestoreData;
            nameToEnabled = data.nameToEnabled.ToDictionary(x => x.Key, x => x.Value);
            nameToIndex = data.nameToIndex.ToDictionary(x => x.Key, x => x.Value);
        }

        #endregion
    }
}
