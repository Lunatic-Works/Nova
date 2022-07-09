using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public abstract class CompositeSpriteController : FadeController, IRestorable
    {
        private const char poseStringSeparator = ':';

        public CompositeSpriteMerger mergerPrimary;
        public CompositeSpriteMerger mergerSub;
        public string imageFolder;
        public string luaGlobalName;

        protected List<string> curPose = new List<string>();
        private DialogueState dialogueState;
        protected GameState gameState;

        protected bool needRender => mergerPrimary.spriteCount > 0 || (isFading && mergerSub.spriteCount > 0);
        protected override string fadeShader => "Nova/Premul/Fade Global";
        public abstract bool renderToCamera { get; }
        public abstract RenderTexture renderTexture { get; }

        // the actually layer of this object
        // if layer = -1, render without considering camera's culling mask
        public virtual int layer => -1;

        protected override void Awake()
        {
            base.Awake();
            var controller = Utils.FindNovaGameController();
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

        public void SetPose(IEnumerable<string> pose, bool fade = true)
        {
            if (curPose.SequenceEqual(pose))
            {
                return;
            }

            fade = fade && !gameState.isRestoring && !dialogueState.isFastForward;
            if (fade)
            {
                mergerSub.SetTextures(mergerPrimary);
            }

            var sprites = LoadPoseSprites(imageFolder, pose);
            mergerPrimary.SetTextures(sprites);
            if (fade)
            {
                FadeAnimation(fadeDuration);
            }

            curPose.Clear();
            curPose.AddRange(pose);
        }

        public void SetPose(string pose, bool fade = true)
        {
            SetPose(pose.Split(poseStringSeparator), fade);
        }

        public void SetPose(LuaInterface.LuaTable pose, bool fade = true)
        {
            SetPose(pose.ToArray().Cast<string>(), fade);
        }

        public void ClearImage(bool fade = true)
        {
            SetPose(Enumerable.Empty<string>(), fade);
        }

        public static string PoseToString(IEnumerable<string> pose)
        {
            return string.Join(poseStringSeparator.ToString(), pose);
        }

        public static string PoseToString(LuaInterface.LuaTable pose)
        {
            return PoseToString(pose.ToArray().Cast<string>());
        }

        public static IReadOnlyList<SpriteWithOffset> LoadPoseSprites(string imageFolder, IEnumerable<string> pose)
        {
            return pose.Select(x => AssetLoader.Load<SpriteWithOffset>(System.IO.Path.Combine(imageFolder, x))).ToList();
        }

        public static IReadOnlyList<SpriteWithOffset> LoadPoseSprites(string imageFolder, string pose)
        {
            return LoadPoseSprites(imageFolder, pose.Split(poseStringSeparator));
        }

        public virtual string restorableName => luaGlobalName;

        [Serializable]
        protected class CompositeSpriteControllerRestoreData : IRestoreData
        {
            public readonly TransformData transform;
            public readonly List<string> poseArray;
            public readonly Vector4Data color;
            public readonly int renderQueue;

            public CompositeSpriteControllerRestoreData(CompositeSpriteController parent)
            {
                this.transform = new TransformData(parent.transform);
                this.poseArray = new List<string>(parent.curPose);
                this.color = parent.color;
                this.renderQueue = parent.gameObject.Ensure<RenderQueueOverrider>().renderQueue;
            }

            public CompositeSpriteControllerRestoreData(CompositeSpriteControllerRestoreData other)
            {
                this.transform = other.transform;
                this.poseArray = other.poseArray;
                this.color = other.color;
                this.renderQueue = other.renderQueue;
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
            SetPose(data.poseArray, false);
        }
    }
}
