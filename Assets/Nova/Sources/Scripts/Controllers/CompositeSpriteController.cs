using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class CompositeSpriteController : OverlayTextureChangerBase
    {
        public const int mergerLayer = 16;
        public GameObject overlay;
        public CompositeSpriteRenderTarget mergerPrimary;
        public CompositeSpriteRenderTarget mergerSub;
        public string imageFolder;
        public string luaGlobalName;
        private static Mesh quad;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private GameState gameState;
        private DialogueBoxController dialogueBoxController;

        protected override string fadeShader => "Nova/VFX/Change Overlay With Fade";

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
            meshRenderer.material = material;

            gameState = Utils.FindNovaGameController().GameState;
            dialogueBoxController = Utils.FindViewManager().GetController<DialogueBoxController>();

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                // gameState.AddRestorable(this);
            }
        }

        public void SetPose(IEnumerable<string> pose, bool fade = true)
        {
            fade = fade && !gameState.isRestoring && dialogueBoxController.state != DialogueBoxState.FastForward;
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
        }

        public void SetPose(LuaInterface.LuaTable pose, bool fade = true)
        {
            SetPose(pose.ToArray().Cast<string>(), fade);
        }

        public void ClearImage(bool fade = true)
        {
            SetPose(Enumerable.Empty<string>(), fade);
        }
    }
}
