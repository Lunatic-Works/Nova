using UnityEngine;

namespace Nova
{
    public interface IOverlayRenderer
    {
        GameObject overlay { get; }
    }

    public class OverlaySpriteController : CompositeSpriteController, IOverlayRenderer
    {
        private const string OverlayShader = "Nova/Premul/Overlay";

        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

        public GameObject overlayObject;
        public GameObject overlay => overlayObject;

        private static Mesh quad;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private MyTarget myTarget;
        private Material overlayMaterial;

        public override bool renderToCamera => false;
        public override RenderTexture renderTexture => myTarget?.targetTexture;

        public override int layer
        {
            get => overlay.layer;
            set => overlay.layer = value;
        }

        protected override void Awake()
        {
            if (quad == null)
            {
                quad = new Mesh
                {
                    vertices = new[]
                    {
                        new Vector3(-1, -1, 0),
                        new Vector3(1, -1, 0),
                        new Vector3(-1, 1, 0),
                        new Vector3(1, 1, 0),
                    },
                    uv = new[]
                    {
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                        new Vector2(0, 1),
                        new Vector2(1, 1),
                    },
                    triangles = new[]
                    {
                        0, 2, 1,
                        2, 3, 1
                    },
                    // some very large bound to disable culling
                    bounds = new Bounds(Vector3.zero, 1e6f * Vector3.one)
                };
            }

            meshFilter = overlay.Ensure<MeshFilter>();
            meshFilter.mesh = quad;
            meshRenderer = overlay.Ensure<MeshRenderer>();
            base.Awake();
            overlayMaterial = materialPool.Get(OverlayShader);
            materialPool.defaultMaterial = null;
            meshRenderer.material = overlayMaterial;

            myTarget = new MyTarget(this);
            myTarget.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            myTarget.OnDestroy();
        }

        protected virtual void Update()
        {
            myTarget.Update();
        }

        protected class MyTarget : RenderTarget
        {
            private new const string SUFFIX = "Composite" + RenderTarget.SUFFIX;

            private readonly OverlaySpriteController parent;

            public override string textureName => parent == null ? oldConfig.name : parent.luaGlobalName + SUFFIX;
            public override bool isFinal => false;
            public override bool isActive => parent != null && parent.needRender;

            public override RenderTexture targetTexture
            {
                set
                {
                    base.targetTexture = value;
                    if (parent != null)
                    {
                        parent.overlayMaterial.SetTexture(MainTexID, value);
                        parent.overlay.SetActive(value != null);
                    }
                }
            }

            public MyTarget(OverlaySpriteController parent)
            {
                this.parent = parent;
            }
        }
    }
}
