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

        protected string currentPose;
        private DialogueState dialogueState;
        protected GameState gameState;

        protected bool needRender => mergerPrimary.spriteCount > 0 || (isFading && mergerSub.spriteCount > 0);
        protected override string fadeShader => "Nova/Premul/Fade Global";
        public abstract bool renderToCamera { get; }
        public abstract RenderTexture renderTexture { get; }

        // the actual layer of this object
        // if layer = -1, render without considering camera's culling mask
        public virtual int layer { get; set; } = -1;

        protected override void Init()
        {
            if (inited)
            {
                return;
            }

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;

            base.Init();
        }

        public void SetPose(string pose, bool fade, float duration)
        {
            if (pose == currentPose)
            {
                return;
            }

            Init();
            fade = fade && !gameState.isRestoring && !dialogueState.isFastForward;
            if (fade)
            {
                mergerSub.SetTextures(mergerPrimary);
            }

            var sprites = LoadSprites(imageFolder, pose);
            mergerPrimary.SetTextures(sprites);
            if (fade)
            {
                DoFadeAnimation(duration);
            }

            currentPose = pose;
        }

        public void SetPose(string pose, bool fade = true)
        {
            SetPose(pose, fade, fadeDuration);
        }

        public void ClearImage(bool fade, float duration)
        {
            SetPose("", fade, duration);
        }

        public void ClearImage(bool fade = true)
        {
            SetPose("", fade, fadeDuration);
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

        public abstract string restorableName { get; }

        [Serializable]
        protected class CompositeSpriteControllerRestoreData : IRestoreData
        {
            public readonly string pose;
            public readonly TransformData transform;
            public readonly Vector4Data color;

            public CompositeSpriteControllerRestoreData(CompositeSpriteController parent)
            {
                pose = parent.currentPose;
                transform = new TransformData(parent.transform);
                color = parent.color;
            }
        }

        public virtual IRestoreData GetRestoreData()
        {
            return new CompositeSpriteControllerRestoreData(this);
        }

        public virtual void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CompositeSpriteControllerRestoreData;
            data.transform.Restore(transform);
            color = data.color;
            SetPose(data.pose, false);
        }

        #endregion
    }
}
