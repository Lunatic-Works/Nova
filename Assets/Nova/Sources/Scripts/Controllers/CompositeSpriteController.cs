using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class CompositeSpriteController : OverlayTextureChangerBase, IRestorable, IOverlayRenderer
    {
        public const int mergerLayer = 16;
        private const string overlayShader = "Nova/VFX/Overlay";

        public GameObject overlay;
        public GameObject overlayObject => overlay;
        public CompositeSpriteMerger mergerPrimary;
        public CompositeSpriteMerger mergerSub;
        public string imageFolder;
        public string luaGlobalName;

        private List<string> curPose = new List<string>();
        private static Mesh quad;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        protected GameState gameState;
        private DialogueState dialogueState;
        private MyTarget myTarget;
        private Material overlayMaterial;

        protected override string fadeShader => "Nova/VFX/Fade Global";
        public RenderTexture renderTexture => myTarget == null ? null : myTarget.targetTexture;

        protected override void Awake()
        {
            if (quad == null)
            {
                quad = new Mesh();
                quad.vertices = new[]
                {
                    new Vector3(-1, -1, 0),
                    new Vector3( 1, -1, 0),
                    new Vector3(-1,  1, 0),
                    new Vector3( 1,  1, 0),
                };
                quad.uv = new[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                };
                quad.triangles = new[]
                {
                    0, 2, 1,
                    2, 3, 1
                };
                // some very large bound to disable culling
                quad.bounds = new Bounds(Vector3.zero, 1e6f * Vector3.one);
            }
            meshFilter = overlay.Ensure<MeshFilter>();
            meshFilter.mesh = quad;
            meshRenderer = overlay.Ensure<MeshRenderer>();
            base.Awake();
            overlayMaterial = materialPool.Get(overlayShader);
            materialPool.defaultMaterial = null;
            meshRenderer.material = overlayMaterial;

            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }

            myTarget = new MyTarget(this);
            myTarget.Awake();
        }

        protected virtual void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }
            myTarget.OnDestroy();
        }

        protected virtual void Update()
        {
            myTarget.Update();
        }

        public void SetPose(IEnumerable<string> pose, bool fade = true)
        {
            fade = fade && !gameState.isRestoring && !dialogueState.isFastForward;
            if (fade)
            {
                mergerSub.SetTextures(mergerPrimary);
            }
            var sprites = pose.Select(x =>
                AssetLoader.Load<SpriteWithOffset>(System.IO.Path.Combine(imageFolder, x))).ToList();
            mergerPrimary.SetTextures(sprites);
            if (fade)
            {
                FadeAnimation(fadeDuration);
            }
            curPose.Clear();
            curPose.AddRange(pose);
        }

        public void SetPose(LuaInterface.LuaTable pose, bool fade = true)
        {
            SetPose(pose.ToArray().Cast<string>(), fade);
        }

        public void ClearImage(bool fade = true)
        {
            SetPose(Enumerable.Empty<string>(), fade);
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

        protected class MyTarget : RenderTarget
        {
            private new const string SUFFIX = "Composite" + RenderTarget.SUFFIX;
            private CompositeSpriteController parent;
            public override string textureName => parent == null ? oldConfig.name : parent.luaGlobalName + SUFFIX;
            public override bool isFinal => false;
            public override bool isActive =>
                parent != null && (parent.mergerPrimary.spriteCount > 0 || (parent.isFading && parent.mergerSub.spriteCount > 0));

            public override RenderTexture targetTexture
            {
                set
                {
                    base.targetTexture = value;
                    if (parent != null)
                    {
                        parent.overlayMaterial.SetTexture("_MainTex", value);
                        parent.overlay.SetActive(value != null);
                    }
                }
            }

            public MyTarget(CompositeSpriteController parent)
            {
                this.parent = parent;
            }
        }
    }
}
