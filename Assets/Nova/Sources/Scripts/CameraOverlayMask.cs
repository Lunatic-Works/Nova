using System;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    [RequireComponent(typeof(Camera))]
    public class CameraOverlayMask : MonoBehaviour, IRestorable
    {
        private static readonly int SubTexID = Shader.PropertyToID("_SubTex");

        public string luaGlobalName;
        public int maskLayer;
        public Camera maskCamera;

        private GameState gameState;
        private Camera masterCamera;
        private RenderTexture renderTexture;

        private Material _blitMaterial;

        public Material blitMaterial
        {
            get => _blitMaterial;
            set
            {
                _blitMaterial = value;
                if (!value) return;
                _blitMaterial.SetTexture(SubTexID, renderTexture);
            }
        }

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            masterCamera = GetComponent<Camera>();

            if (maskCamera)
            {
                maskCamera.cullingMask = 1 << maskLayer;
                // MaskCamera renders first
                maskCamera.depth = masterCamera.depth - 1;
            }

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }
        }

        private void OnDestroy()
        {
            Destroy(renderTexture);

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        private void EnsureRenderTextureSize()
        {
            if (!maskCamera) return;

            if (renderTexture &&
                renderTexture.width == masterCamera.pixelWidth &&
                renderTexture.height == masterCamera.pixelHeight) return;

            Destroy(renderTexture);
            renderTexture = new RenderTexture(masterCamera.pixelWidth, masterCamera.pixelHeight, 24)
            {
                name = $"CameraOverlayMaskRenderTexture({masterCamera.name})"
            };
            maskCamera.targetTexture = renderTexture;
            if (blitMaterial)
            {
                blitMaterial.SetTexture(SubTexID, renderTexture);
            }
        }

        private void Update()
        {
            EnsureRenderTextureSize();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (!maskCamera || !maskCamera.gameObject.activeInHierarchy || !blitMaterial)
            {
                Graphics.Blit(src, dest);
            }
            else
            {
                Graphics.Blit(src, dest, blitMaterial);
            }
        }

        #region Restoration

        public string restorableName => luaGlobalName;

        [Serializable]
        private class CameraOverlayMaskRestoreData : IRestoreData
        {
            public readonly MaterialData materialData;

            public CameraOverlayMaskRestoreData(MaterialData materialData)
            {
                this.materialData = materialData;
            }
        }

        public IRestoreData GetRestoreData()
        {
            // BlitMaterial must be RestorableMaterial or null
            MaterialData materialData;
            if (blitMaterial is RestorableMaterial)
            {
                materialData = RestorableMaterial.GetRestoreData(blitMaterial);
            }
            else
            {
                materialData = null;
            }

            return new CameraOverlayMaskRestoreData(materialData);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as CameraOverlayMaskRestoreData;

            // BlitMaterial must be RestorableMaterial or null
            if (data.materialData != null)
            {
                MaterialFactory factory = MaterialPool.Ensure(gameObject).factory;
                blitMaterial = RestorableMaterial.Restore(data.materialData, factory);
            }
            else
            {
                blitMaterial = null;
            }
        }

        #endregion
    }
}