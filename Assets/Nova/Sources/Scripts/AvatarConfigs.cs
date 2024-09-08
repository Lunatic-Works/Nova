using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class AvatarRectConfig
    {
        public string key;
        public Rect rect;
    }

    [Serializable]
    public class AvatarConfig
    {
        public string characterName;
        public GameCharacterController characterController;
        public List<AvatarRectConfig> rects;
    }

    public class AvatarConfigs : MonoBehaviour
    {
        [SerializeField] private List<AvatarConfig> configs;

        private readonly Dictionary<string, AvatarConfig> nameToConfig = new Dictionary<string, AvatarConfig>();

        private void Awake()
        {
            foreach (var config in configs)
            {
                nameToConfig[config.characterName] = config;
            }
        }

        public bool Contains(string characterName)
        {
            return nameToConfig.ContainsKey(characterName);
        }

        public GameCharacterController GetCharacterController(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                return null;
            }

            if (nameToConfig.TryGetValue(characterName, out var config))
            {
                return config.characterController;
            }

            return null;
        }

        public string GetImageFolder(string characterName)
        {
            return GetCharacterController(characterName)?.imageFolder ?? null;
        }

        public Rect GetRect(string characterName, string pose)
        {
            if (nameToConfig.TryGetValue(characterName, out var config))
            {
                foreach (var rectConfig in config.rects)
                {
                    if (pose.StartsWith(rectConfig.key, StringComparison.Ordinal))
                    {
                        return rectConfig.rect;
                    }
                }

                Debug.LogWarning($"Nova: Cannot find avatar rect for character {characterName} pose {pose}");
                return Rect.zero;
            }

            Debug.LogWarning($"Nova: GetRect with unknown characterName {characterName}");
            return Rect.zero;
        }
    }
}
