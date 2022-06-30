using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [Serializable]
    public class AvatarConfig
    {
        public string characterName;
        public GameCharacterController characterController;
        public string prefix;
    }

    [ExportCustomType]
    [RequireComponent(typeof(RawImage))]
    public class AvatarController : CompositeSpriteController
    {
        public List<AvatarConfig> avatarConfigs;
        public int textPadding;
        public Camera renderCamera;

        private RawImage image;
        private RectTransform rectTransform;
        private readonly Dictionary<string, AvatarConfig> nameToConfig = new Dictionary<string, AvatarConfig>();
        private string characterName;
        private Dictionary<string, string> characterToImageName = new Dictionary<string, string>();

        public int textPaddingOrZero => curPose.Any() ? textPadding : 0;
        public override bool renderToCamera => true;
        public override RenderTexture renderTexture => null;

        protected override void Awake()
        {
            base.Awake();
            image = GetComponent<RawImage>();
            rectTransform = GetComponent<RectTransform>();
            foreach (var config in avatarConfigs)
            {
                nameToConfig[config.characterName] = config;
            }
        }

        private void Start()
        {
            var referenceWidth = (int)rectTransform.rect.width;
            var referenceHeight = (int)rectTransform.rect.height;
            var rt = new RenderTexture(referenceWidth, referenceHeight, 0, RenderTextureFormat.ARGB32);
            rt.name = "AvatarTexture";
            renderCamera.targetTexture = rt;
            image.texture = rt;
            renderCamera.enabled = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(renderCamera.targetTexture);
        }

        public void SetCharacterName(string name)
        {
            characterName = name;
        }

        private bool CheckCharacterName(string imageName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Debug.LogWarning($"Nova: Set avatar {imageName} with empty characterName.");
                return false;
            }

            if (!nameToConfig.ContainsKey(characterName))
            {
                Debug.LogWarning($"Nova: Set avatar {imageName} with unknown characterName {characterName}");
                return false;
            }

            return true;
        }

        public GameCharacterController GetCharacterController()
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
            if (!CheckCharacterName("<pose>"))
            {
                return;
            }

            var prefix = nameToConfig[characterName].prefix;
            var poseStr = PoseToString(pose.ToArray().Cast<string>().Select(x => prefix + x).ToArray());
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
            if (!CheckCharacterName("<clear>"))
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
                SetPose(characterToImageName[characterName], fade);
            }
        }

        public void ResetAll()
        {
            characterToImageName.Clear();
        }

        private void Update()
        {
            // TODO: optimize this by implementing SetPose callbacks
            renderCamera.enabled = needRender;
        }

        #region Restoration

        [Serializable]
        private class AvatarControllerRestoreData : CompositeSpriteControllerRestoreData
        {
            // No need to save characterName, because it will be set in the action of the dialogue entry
            public readonly Dictionary<string, string> characterToImageName;

            public AvatarControllerRestoreData(CompositeSpriteControllerRestoreData baseData,
                Dictionary<string, string> characterToImageName) : base(baseData)
            {
                this.characterToImageName = characterToImageName;
            }
        }

        public override IRestoreData GetRestoreData()
        {
            return new AvatarControllerRestoreData(base.GetRestoreData() as CompositeSpriteControllerRestoreData,
                characterToImageName);
        }

        public override void Restore(IRestoreData restoreData)
        {
            // Avoid updating image when restoring base by setting currentImageName = baseData.currentImageName
            // var baseData = restoreData as CompositeSpriteControllerRestoreData;
            // var currentImageNameOld = currentImageName;
            // currentImageName = baseData.currentImageName;
            base.Restore(restoreData);
            // currentImageName = currentImageNameOld;

            var data = restoreData as AvatarControllerRestoreData;
            characterToImageName = data.characterToImageName;
        }

        #endregion
    }
}
