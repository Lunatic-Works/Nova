using System;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public abstract class CompositeSpriteControllerBase : MonoBehaviour, IRestorable
    {
        public string imageFolder;
        public SpriteMerger characterTextureMerger;

        public string currentImageName { get; protected set; }

        public OverlayTextureChangerBase textureChanger { get; protected set; }

        protected GameState gameState;
        private SpriteRenderer spriteRenderer;
        private SpriteChangerWithFade spriteChanger;
        private DialogueBoxController dialogueBoxController;

        protected virtual void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteChanger = GetComponent<SpriteChangerWithFade>();
            textureChanger = GetComponent<OverlayTextureChangerBase>();
            dialogueBoxController = GameObject.FindWithTag("DialogueView").GetComponent<DialogueBoxController>();
        }

        #region Color

        public abstract Color color { get; set; }

        protected void SetColor(Color color)
        {
            if (textureChanger != null)
            {
                textureChanger.color = color;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        #endregion

        #region Pose

        protected static string PoseArrayToString(string[] poseArray)
        {
            return string.Join(":", poseArray);
        }

        protected static string[] StringToPoseArray(string s)
        {
            return s.Split(':');
        }

        protected void SetPose(string[] poseArray, bool fade)
        {
            string poseName = PoseArrayToString(poseArray);
            if (poseName == currentImageName)
            {
                return;
            }

            var sprites = poseArray.Select(imageName =>
                AssetLoader.Load<SpriteWithOffset>(System.IO.Path.Combine(imageFolder, imageName))).ToList();
            var texture = characterTextureMerger.GetMergedTexture(name, sprites);
            if (fade && !gameState.isMovingBack && dialogueBoxController.state != DialogueBoxState.FastForward)
            {
                textureChanger.SetTexture(texture);
            }
            else
            {
                textureChanger.SetTextureNoFade(texture);
            }

            currentImageName = poseName;
        }

        protected void SetImageOrPose(string imageName, bool fade = true)
        {
            if (imageName == currentImageName)
            {
                return;
            }

            if (string.IsNullOrEmpty(imageName))
            {
                ClearImage(fade);
                return;
            }

            string[] parts = StringToPoseArray(imageName);
            if (parts.Length == 1)
            {
                SetImage(imageName, fade);
            }
            else
            {
                SetPose(parts, fade);
            }
        }

        #endregion

        #region Methods called by external scripts

        public void PreloadPose(LuaInterface.LuaTable pose)
        {
            foreach (string imageName in pose.ToArray().Cast<string>())
            {
                AssetLoader.Preload(AssetCacheType.StandingLayer, System.IO.Path.Combine(imageFolder, imageName));
            }
        }

        public void UnpreloadPose(LuaInterface.LuaTable pose)
        {
            foreach (string imageName in pose.ToArray().Cast<string>())
            {
                AssetLoader.Unpreload(AssetCacheType.StandingLayer, System.IO.Path.Combine(imageFolder, imageName));
            }
        }

        public void SetPose(LuaInterface.LuaTable pose, bool fade = true)
        {
            this.RuntimeAssert(characterTextureMerger != null && textureChanger != null,
                "CharacterTextureMerger and OverlayTextureChanger must be present when setting pose.");
            SetPose(pose.ToArray().Cast<string>().ToArray(), fade);
        }

        public void SetImage(string imageName, bool fade = true)
        {
            this.RuntimeAssert(characterTextureMerger == null && textureChanger == null,
                "Do not use SetImage() when CharacterTextureMerger or OverlayTextureChanger exists. " +
                $"If you want to use SetPose(), check imageName: {imageName}");

            if (imageName == currentImageName)
            {
                return;
            }

            Sprite sprite = AssetLoader.Load<Sprite>(System.IO.Path.Combine(imageFolder, imageName));
            if (spriteChanger != null && spriteChanger.enabled && fade && !gameState.isMovingBack &&
                dialogueBoxController.state != DialogueBoxState.FastForward)
            {
                // If pose is not changing, do not hide body
                if (currentImageName != null && imageName[0] == currentImageName[0])
                {
                    spriteChanger.SetSprite(sprite, overlay: true);
                }
                else
                {
                    spriteChanger.sprite = sprite;
                }
            }
            else
            {
                spriteRenderer.sprite = sprite;
            }

            currentImageName = imageName;
        }

        public void ClearImage(bool fade = true)
        {
            if (string.IsNullOrEmpty(currentImageName))
            {
                return;
            }

            if (textureChanger != null)
            {
                if (fade && !gameState.isMovingBack && dialogueBoxController.state != DialogueBoxState.FastForward)
                {
                    textureChanger.SetTexture(null);
                }
                else
                {
                    textureChanger.SetTextureNoFade(null);
                }
            }
            else if (spriteChanger != null && spriteChanger.enabled && fade && !gameState.isMovingBack &&
                     dialogueBoxController.state != DialogueBoxState.FastForward)
            {
                spriteChanger.sprite = null;
            }
            else
            {
                spriteRenderer.sprite = null;
            }

            currentImageName = null;

            characterTextureMerger.ReleaseCache(name);
        }

        #endregion

        #region Restoration

        [Serializable]
        protected class CompositeSpriteControllerBaseRestoreData : IRestoreData
        {
            public readonly string currentImageName;
            public readonly TransformRestoreData transformRestoreData;
            public readonly Vector4Data color;
            public readonly MaterialRestoreData materialRestoreData;
            public readonly int renderQueue;

            public CompositeSpriteControllerBaseRestoreData(string currentImageName, Transform transform, Color color,
                MaterialRestoreData materialRestoreData, int renderQueue)
            {
                this.currentImageName = currentImageName;
                transformRestoreData = new TransformRestoreData(transform);
                this.color = color;
                this.materialRestoreData = materialRestoreData;
                this.renderQueue = renderQueue;
            }

            public CompositeSpriteControllerBaseRestoreData(CompositeSpriteControllerBaseRestoreData baseData)
            {
                currentImageName = baseData.currentImageName;
                transformRestoreData = baseData.transformRestoreData;
                color = baseData.color;
                materialRestoreData = baseData.materialRestoreData;
                renderQueue = baseData.renderQueue;
            }
        }

        public abstract string restorableObjectName { get; }

        public virtual IRestoreData GetRestoreData()
        {
            // Material must be RestorableMaterial or DefaultMaterial
            MaterialRestoreData materialRestoreData;
            if (spriteRenderer != null && spriteRenderer.sharedMaterial is RestorableMaterial)
            {
                materialRestoreData = RestorableMaterial.GetRestoreData(spriteRenderer.sharedMaterial);
            }
            else
            {
                materialRestoreData = null;
            }

            int renderQueue = RenderQueueOverrider.Ensure(gameObject).renderQueue;

            return new CompositeSpriteControllerBaseRestoreData(currentImageName, transform, color, materialRestoreData,
                renderQueue);
        }

        public virtual void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CompositeSpriteControllerBaseRestoreData;
            data.transformRestoreData.Restore(transform);
            color = data.color;

            if (spriteRenderer != null)
            {
                // Material must be RestorableMaterial or DefaultMaterial
                if (data.materialRestoreData != null)
                {
                    MaterialFactory factory = MaterialPool.Ensure(gameObject).factory;
                    spriteRenderer.material =
                        RestorableMaterial.RestoreMaterialFromData(data.materialRestoreData, factory);
                }
                else
                {
                    spriteRenderer.material = MaterialPool.Ensure(gameObject).defaultMaterial;
                }
            }

            RenderQueueOverrider.Ensure(gameObject).renderQueue = data.renderQueue;

            SetImageOrPose(data.currentImageName, false);
        }

        #endregion
    }
}