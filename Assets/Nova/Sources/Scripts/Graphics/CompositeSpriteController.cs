using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public abstract class CompositeSpriteController : FadeController, IRestorable
    {
        private const char PoseStringSeparator = '+';

        public CompositeSpriteMerger mergerPrimary;
        public CompositeSpriteMerger mergerSub;
        public string imageFolder;
        public string luaGlobalName;

        protected string currentPose;
        private DialogueState dialogueState;
        protected GameState gameState;

        protected bool needRender => mergerPrimary.spriteCount > 0 || (isFading && mergerSub.spriteCount > 0);
        protected override string fadeShader => "Nova/Premul/Fade Global";
        public abstract bool renderToCamera { get; }
        public abstract RenderTexture renderTexture { get; }

        // the actually layer of this object
        // if layer = -1, render without considering camera's culling mask
        public virtual int layer { get; set; } = -1;

        protected override void Awake()
        {
            base.Awake();
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        public void SetPose(string pose, bool fade = true)
        {
            if (pose == currentPose)
            {
                return;
            }

            fade = fade && !gameState.isRestoring && !dialogueState.isFastForward;
            if (fade)
            {
                mergerSub.SetTextures(mergerPrimary);
            }

            var sprites = LoadSprites(imageFolder, pose);
            mergerPrimary.SetTextures(sprites);
            if (fade)
            {
                FadeAnimation(fadeDuration);
            }

            currentPose = pose;
        }

        public void ClearImage(bool fade = true)
        {
            SetPose("", fade);
        }

        public static string ArrayToPose(IEnumerable<string> pose)
        {
            return string.Join(PoseStringSeparator.ToString(), pose);
        }

        public static IEnumerable<string> PoseToArray(string pose)
        {
            return string.IsNullOrEmpty(pose) ? Enumerable.Empty<string>() : pose.Split(PoseStringSeparator);
        }

        public static IReadOnlyList<SpriteWithOffset> LoadSprites(string imageFolder, string pose)
        {
            return PoseToArray(pose)
                .Select(x => AssetLoader.Load<SpriteWithOffset>(System.IO.Path.Combine(imageFolder, x)))
                .ToList();
        }

        public void Preload(AssetCacheType type, string pose)
        {
            foreach (var x in PoseToArray(pose))
            {
                AssetLoader.Preload(type, System.IO.Path.Combine(imageFolder, x));
            }
        }

        public void Unpreload(AssetCacheType type, string pose)
        {
            foreach (var x in PoseToArray(pose))
            {
                AssetLoader.Unpreload(type, System.IO.Path.Combine(imageFolder, x));
            }
        }

        #region Restoration

        public virtual string restorableName => luaGlobalName;

        [Serializable]
        protected class CompositeSpriteControllerRestoreData : IRestoreData
        {
            public readonly TransformData transform;
            public readonly string pose;
            public readonly Vector4Data color;
            public readonly int renderQueue;

            public CompositeSpriteControllerRestoreData(CompositeSpriteController parent)
            {
                transform = new TransformData(parent.transform);
                pose = parent.currentPose;
                color = parent.color;
                renderQueue = parent.gameObject.Ensure<RenderQueueOverrider>().renderQueue;
            }

            public CompositeSpriteControllerRestoreData(CompositeSpriteControllerRestoreData other)
            {
                transform = other.transform;
                pose = other.pose;
                color = other.color;
                renderQueue = other.renderQueue;
            }
        }

        public virtual IRestoreData GetRestoreData()
        {
            return new CompositeSpriteControllerRestoreData(this);
        }

        public virtual void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CompositeSpriteControllerRestoreData;
            data.transform.Restore(this.transform);
            color = data.color;
            gameObject.Ensure<RenderQueueOverrider>().renderQueue = data.renderQueue;
            SetPose(data.pose, false);
        }

        #endregion
    }
}
