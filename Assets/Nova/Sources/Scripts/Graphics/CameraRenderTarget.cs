using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class CameraRenderTarget : MonoBehaviour
    {
        public bool isFinalTarget;
        public List<RawImage> images = new List<RawImage>();
        private Camera renderCamera;
        private MyTarget target;

        private void Awake()
        {
            renderCamera = GetComponent<Camera>();
            target = new MyTarget(this);
            target.Awake();
        }

        private void OnDestroy()
        {
            if (target != null)
            {
                target.OnDestroy();
                target = null;
            }
        }

        private void EnsureTarget()
        {
#if UNITY_EDITOR
            if (renderCamera == null)
            {
                renderCamera = GetComponent<Camera>();
            }

            if (target == null)
            {
                target = Utils.FindRenderManager().GetRenderTarget(renderCamera.name + RenderTarget.SUFFIX) as MyTarget;
                if (target == null)
                {
                    target = new MyTarget(this);
                    target.Awake();
                }
            }
#endif
        }

        private void Update()
        {
            // after recompile in Editor mode, private bindings are lost to null
            EnsureTarget();
            target.Update();
        }

        private class MyTarget : RenderTarget
        {
            private readonly CameraRenderTarget parent;
            public override bool isActive => parent.renderCamera != null && parent.renderCamera.isActiveAndEnabled;
            public override bool isFinal => parent.isFinalTarget;
            public override string textureName => parent == null ? oldConfig.name : parent.renderCamera.name + SUFFIX;

            public override RenderTexture targetTexture
            {
                get => _targetTexture;
                set
                {
                    base.targetTexture = value;
                    foreach (var img in parent.images.Where(x => x != null))
                    {
                        img.texture = _targetTexture;
                    }

                    if (parent.renderCamera != null)
                    {
                        parent.renderCamera.targetTexture = _targetTexture;
                    }
                }
            }

            public MyTarget(CameraRenderTarget parent)
            {
                this.parent = parent;
            }
        }
    }
}
