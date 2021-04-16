using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class AutoVoiceConfig
    {
        public string characterName;
        public CharacterController characterController;
        public string prefix;
    }

    [ExportCustomType]
    public class AutoVoiceConfigs : MonoBehaviour
    {
        private readonly Dictionary<string, CharacterController> nameToCharacterController =
            new Dictionary<string, CharacterController>();

        private readonly Dictionary<string, string> nameToVoicePrefix = new Dictionary<string, string>();

        public string luaGlobalName;
        public AutoVoiceConfig[] autoVoiceConfigs;

        public string restorableObjectName => luaGlobalName;

        public void Awake()
        {
            LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
            foreach (var config in autoVoiceConfigs)
            {
                nameToCharacterController.Add(config.characterName, config.characterController);
                nameToVoicePrefix.Add(config.characterName, config.prefix);
            }
        }

        public CharacterController GetCharacterControllerForName(string characterName)
        {
            if (nameToCharacterController.TryGetValue(characterName, out CharacterController value))
            {
                return value;
            }

            return null;
        }

        public string GetVoicePrefixForName(string characterName)
        {
            if (nameToVoicePrefix.TryGetValue(characterName, out string value))
            {
                return value;
            }

            return "";
        }
    }
}