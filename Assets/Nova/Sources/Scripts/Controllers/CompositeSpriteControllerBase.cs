using System;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [RequireComponent(typeof(OverlayTextureChangerBase))]
    public abstract class CompositeSpriteControllerBase : MonoBehaviour, IRestorable
    {
        public string imageFolder;
        public SpriteMerger spriteMerger;

        public string currentImageName { get; protected set; }
        public OverlayTextureChangerBase textureChanger { get; protected set; }

        protected GameState gameState;
        private DialogueState dialogueState;

        protected virtual void Awake()
        {
            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;
            textureChanger = GetComponent<OverlayTextureChangerBase>();
        }

        #region Color

        public abstract Color color { get; set; }

        protected void SetColor(Color color)
        {
            textureChanger.color = color;
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
            var texture = spriteMerger.GetMergedTexture(name, sprites);
            if (fade && !gameState.isRestoring && !dialogueState.isFastForward)
            {
                textureChanger.SetTexture(texture);
            }
            else
            {
                textureChanger.SetTextureNoFade(texture);
            }

            currentImageName = poseName;
        }

        protected void SetPose(string imageName, bool fade)
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
            SetPose(parts, fade);
        }

        #endregion

        #region Methods called by external scripts

        public void PreloadPose(LuaInterface.LuaTable pose)
        {
            foreach (string imageName in pose.ToArray().Cast<string>())
            {
                AssetLoader.Preload(AssetCacheType.Standing, System.IO.Path.Combine(imageFolder, imageName));
            }
        }

        public void UnpreloadPose(LuaInterface.LuaTable pose)
        {
            foreach (string imageName in pose.ToArray().Cast<string>())
            {
                AssetLoader.Unpreload(AssetCacheType.Standing, System.IO.Path.Combine(imageFolder, imageName));
            }
        }

        public void SetPose(LuaInterface.LuaTable pose, bool fade = true)
        {
            this.RuntimeAssert(spriteMerger != null && textureChanger != null,
                "SpriteMerger and OverlayTextureChangerBase must be present when setting pose.");
            SetPose(pose.ToArray().Cast<string>().ToArray(), fade);
        }

        public void ClearImage(bool fade = true)
        {
            if (string.IsNullOrEmpty(currentImageName))
            {
                return;
            }

            if (fade && !gameState.isRestoring && !dialogueState.isFastForward)
            {
                textureChanger.SetTexture(null);
            }
            else
            {
                textureChanger.SetTextureNoFade(null);
            }

            currentImageName = null;

            spriteMerger.ReleaseCache(name);
        }

        #endregion

        #region Restoration

        public abstract string restorableName { get; }

        [Serializable]
        protected class CompositeSpriteControllerBaseRestoreData : IRestoreData
        {
            public readonly string currentImageName;
            public readonly TransformData transformData;
            public readonly Vector4Data color;
            public readonly int renderQueue;

            public CompositeSpriteControllerBaseRestoreData(string currentImageName, Transform transform, Color color,
                int renderQueue)
            {
                this.currentImageName = currentImageName;
                transformData = new TransformData(transform);
                this.color = color;
                this.renderQueue = renderQueue;
            }

            public CompositeSpriteControllerBaseRestoreData(CompositeSpriteControllerBaseRestoreData baseData)
            {
                currentImageName = baseData.currentImageName;
                transformData = baseData.transformData;
                color = baseData.color;
                renderQueue = baseData.renderQueue;
            }
        }

        public virtual IRestoreData GetRestoreData()
        {
            int renderQueue = RenderQueueOverrider.Ensure(gameObject).renderQueue;
            return new CompositeSpriteControllerBaseRestoreData(currentImageName, transform, color, renderQueue);
        }

        public virtual void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CompositeSpriteControllerBaseRestoreData;
            data.transformData.Restore(transform);
            color = data.color;
            RenderQueueOverrider.Ensure(gameObject).renderQueue = data.renderQueue;
            SetPose(data.currentImageName, false);
        }

        #endregion
    }
}
