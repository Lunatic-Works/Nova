using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class AvatarConfig
    {
        public string characterName;
        public CharacterController characterController;
        public string prefix;
    }

    [ExportCustomType]
    public class AvatarController : CompositeSpriteControllerBase
    {
        public string luaGlobalName;
        public List<AvatarConfig> avatarConfigs;
        public int textPadding = 200;

        public int textPaddingOrZero => string.IsNullOrEmpty(currentImageName) ? 0 : textPadding;

        private readonly Dictionary<string, AvatarConfig> nameToConfig = new Dictionary<string, AvatarConfig>();

        private string characterName;
        private Dictionary<string, string> characterToImageName = new Dictionary<string, string>();

        protected override void Awake()
        {
            base.Awake();

            foreach (var config in avatarConfigs)
            {
                nameToConfig[config.characterName] = config;
            }

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        public void SetCharacterName(string name)
        {
            characterName = name;
        }

        private bool CheckCharacterName(string imageName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Debug.LogWarning($"Nova: Set avatar {imageName} with empty characterName");
                return false;
            }

            if (!nameToConfig.ContainsKey(characterName))
            {
                Debug.LogWarning($"Nova: Set avatar {imageName} with unknown characterName {characterName}");
                return false;
            }

            return true;
        }

        public CharacterController GetCharacterController()
        {
            if (nameToConfig.ContainsKey(characterName))
            {
                return nameToConfig[characterName].characterController;
            }
            else
            {
                Debug.LogWarning($"Nova: GetCharacterController with unknown characterName {characterName}");
                return null;
            }
        }

        public void SetPoseDelayed(LuaInterface.LuaTable pose)
        {
            if (!CheckCharacterName("pose"))
            {
                return;
            }

            var prefix = nameToConfig[characterName].prefix;
            var poseStr = PoseArrayToString(pose.ToArray().Cast<string>().Select(x => prefix + x).ToArray());
            characterToImageName[characterName] = poseStr;
        }

        public void SetImageDelayed(string imageName)
        {
            if (!CheckCharacterName(imageName))
            {
                return;
            }

            characterToImageName[characterName] = nameToConfig[characterName].prefix + imageName;
        }

        public void ClearImageDelayed()
        {
            if (!CheckCharacterName(""))
            {
                return;
            }

            characterToImageName.Remove(characterName);
        }

        public void UpdateImage(bool fade = true)
        {
            if (string.IsNullOrEmpty(characterName) || !nameToConfig.ContainsKey(characterName) ||
                !characterToImageName.ContainsKey(characterName))
            {
                ClearImage(fade);
            }
            else
            {
                SetImageOrPose(characterToImageName[characterName], fade);
            }
        }

        public void ResetAll()
        {
            characterToImageName.Clear();
        }

        private Color _color = Color.white;

        public override Color color
        {
            get => _color;
            set => SetColor(_color = value);
        }

        [Serializable]
        private class AvatarRestoreData : CompositeSpriteControllerBaseRestoreData
        {
            // No need to save characterName, because it will be set in the action of the dialogue entry
            public readonly Dictionary<string, string> characterToImageName;

            public AvatarRestoreData(CompositeSpriteControllerBaseRestoreData baseData,
                Dictionary<string, string> characterToImageName) : base(baseData)
            {
                this.characterToImageName = characterToImageName;
            }
        }

        public override string restorableObjectName => luaGlobalName;

        public override IRestoreData GetRestoreData()
        {
            return new AvatarRestoreData(base.GetRestoreData() as CompositeSpriteControllerBaseRestoreData,
                characterToImageName);
        }

        public override void Restore(IRestoreData restoreData)
        {
            // Avoid updating image when restoring base by setting currentImageName = baseData.currentImageName
            var baseData = restoreData as CompositeSpriteControllerBaseRestoreData;
            var currentImageNameOld = currentImageName;
            currentImageName = baseData.currentImageName;
            base.Restore(baseData);
            currentImageName = currentImageNameOld;

            var data = restoreData as AvatarRestoreData;
            characterToImageName = data.characterToImageName;
        }
    }
}