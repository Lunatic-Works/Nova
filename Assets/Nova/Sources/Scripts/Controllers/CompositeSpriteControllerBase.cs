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
        private DialogueBoxController dialogueBoxController;

        protected virtual void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            dialogueBoxController = Utils.FindViewManager().GetController<DialogueBoxController>();
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
            if (fade && !gameState.isRestoring && dialogueBoxController.state != DialogueBoxState.FastForward)
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

            if (fade && !gameState.isRestoring && dialogueBoxController.state != DialogueBoxState.FastForward)
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

        [Serializable]
        protected class CompositeSpriteControllerBaseRestoreData : IRestoreData
        {
            public readonly string currentImageName;
            public readonly TransformRestoreData transformRestoreData;
            public readonly Vector4Data color;
            public readonly int renderQueue;

            public CompositeSpriteControllerBaseRestoreData(string currentImageName, Transform transform, Color color,
                int renderQueue)
            {
                this.currentImageName = currentImageName;
                transformRestoreData = new TransformRestoreData(transform);
                this.color = color;
                this.renderQueue = renderQueue;
            }

            public CompositeSpriteControllerBaseRestoreData(CompositeSpriteControllerBaseRestoreData baseData)
            {
                currentImageName = baseData.currentImageName;
                transformRestoreData = baseData.transformRestoreData;
                color = baseData.color;
                renderQueue = baseData.renderQueue;
            }
        }

        public abstract string restorableObjectName { get; }

        public virtual IRestoreData GetRestoreData()
        {
            int renderQueue = RenderQueueOverrider.Ensure(gameObject).renderQueue;
            return new CompositeSpriteControllerBaseRestoreData(currentImageName, transform, color, renderQueue);
        }

        public virtual void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CompositeSpriteControllerBaseRestoreData;
            data.transformRestoreData.Restore(transform);
            color = data.color;
            RenderQueueOverrider.Ensure(gameObject).renderQueue = data.renderQueue;
            SetPose(data.currentImageName, false);
        }

        #endregion
    }
}